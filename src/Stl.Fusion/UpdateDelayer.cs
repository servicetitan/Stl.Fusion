using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.Time;

namespace Stl.Fusion
{
    public interface IUpdateDelayer
    {
        Task DelayAsync(IState state, CancellationToken cancellationToken = default);
        Task DelayAsync(int retryCount, CancellationToken cancellationToken = default);
        void CancelDelays(bool immediately = false);
    }

    public interface IUpdateDelayer<T> : IUpdateDelayer { }

    public class UpdateDelayer : IUpdateDelayer
    {
        public static IUpdateDelayer None { get; } = new NoDelayUpdateDelayer();

        public class Options : IOptions
        {
            public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MinExtraErrorDelay { get; set; } =  TimeSpan.FromSeconds(5);
            public TimeSpan MaxExtraErrorDelay { get; set; } = TimeSpan.FromMinutes(2);
            public TimeSpan CancelDelaysDelay { get; set; } = TimeSpan.FromSeconds(0.05);
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        private volatile Task<Unit> _cancelDelaysTask = null!;
        protected Task<Unit> CancelDelaysTask => _cancelDelaysTask;
        protected TimeSpan Delay { get; }
        protected TimeSpan MinExtraErrorDelay { get; }
        protected TimeSpan MaxExtraErrorDelay { get; }
        protected TimeSpan CancelDelaysDelay { get; } // Really sorry for this name :)
        protected IMomentClock Clock { get; }

        public UpdateDelayer(Options? options = null)
        {
            options = options.OrDefault();
            Delay = options.Delay;
            MinExtraErrorDelay = options.MinExtraErrorDelay;
            MaxExtraErrorDelay = options.MaxExtraErrorDelay;
            CancelDelaysDelay = options.CancelDelaysDelay;
            Clock = options.Clock;
            CancelDelays(true);
        }

        public virtual Task DelayAsync(IState state, CancellationToken cancellationToken = default)
            => DelayAsync(state.Snapshot.RetryCount, cancellationToken);
        public virtual async Task DelayAsync(int retryCount, CancellationToken cancellationToken = default)
        {
            var delay = Math.Max(0, Delay.TotalSeconds);
            var start = Clock.Now;

            if (retryCount > 0) {
                var extraDelay = Math.Pow(Math.Sqrt(2), retryCount - 1) * MinExtraErrorDelay.TotalSeconds;
                extraDelay = Math.Min(MaxExtraErrorDelay.TotalSeconds, extraDelay);
                delay += extraDelay;
            }
            if (delay < 0)
                return;
            try {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                await Task.WhenAny(delayTask, CancelDelaysTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            if (retryCount > 0) {
                // If it's an error, we still want to enforce at least
                // the default delay -- even if the delay was cancelled.
                var elapsed = Clock.Now - start;
                if (elapsed < Delay)
                    await Clock.DelayAsync(Delay - elapsed, cancellationToken).ConfigureAwait(false);
            }
        }

        public void CancelDelays(bool immediately = false)
        {
            if (!immediately) {
                Clock
                    .DelayAsync(CancelDelaysDelay, CancellationToken.None)
                    .ContinueWith(_ => CancelDelays(true));
                return;
            }
            var newTask = TaskSource.New<Unit>(true).Task;
            var oldTask = Interlocked.Exchange(ref _cancelDelaysTask, newTask);
            if (oldTask != null)
                TaskSource.For(oldTask).SetResult(default);
        }
    }

    public class UpdateDelayer<T> : UpdateDelayer, IUpdateDelayer<T>
    {
        public new class Options : UpdateDelayer.Options { }

        public UpdateDelayer(Options? options = null) : base(options) { }
    }

    internal class NoDelayUpdateDelayer : IUpdateDelayer
    {
        public Task DelayAsync(IState state, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DelayAsync(int retryCount, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void CancelDelays(bool immediately = false) { }
    }
}
