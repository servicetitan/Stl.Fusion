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
        void CancelDelays(TimeSpan? cancellationDelay = null);
    }

    public interface IUpdateDelayer<T> : IUpdateDelayer { }

    public class UpdateDelayer : IUpdateDelayer
    {
        public class Options : IOptions
        {
            public static Options Default => new Options();
            public static Options InstantUpdates => new Options() {
                Delay = TimeSpan.FromSeconds(0.01),
                MinExtraErrorDelay = TimeSpan.FromSeconds(1),
            };

            public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MinExtraErrorDelay { get; set; } =  TimeSpan.FromSeconds(5);
            public TimeSpan MaxExtraErrorDelay { get; set; } = TimeSpan.FromMinutes(2);
            public TimeSpan CancellationDelay { get; set; } = TimeSpan.FromSeconds(0.05);
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        private volatile Task<Unit> _cancelDelaysTask = null!;
        protected Task<Unit> CancelDelaysTask => _cancelDelaysTask;
        protected IMomentClock Clock { get; }

        public TimeSpan Delay { get; set; }
        public TimeSpan MinExtraErrorDelay { get; set; }
        public TimeSpan MaxExtraErrorDelay { get; set; }
        public TimeSpan CancellationDelay { get; set; }

        public UpdateDelayer(Options? options = null)
        {
            options = options.OrDefault();
            Delay = options.Delay;
            MinExtraErrorDelay = options.MinExtraErrorDelay;
            MaxExtraErrorDelay = options.MaxExtraErrorDelay;
            CancellationDelay = options.CancellationDelay;
            Clock = options.Clock;
            CancelDelays(TimeSpan.Zero);
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
            if (delay <= 0)
                return;

            await CancelDelaysTask
                .WithFakeCancellation(cancellationToken)
                .WithTimeout(TimeSpan.FromSeconds(delay), Clock)
                .ConfigureAwait(false);
            if (retryCount > 0) {
                // If it's an error, we still want to enforce at least
                // the default delay -- even if the delay was cancelled.
                var elapsed = Clock.Now - start;
                if (elapsed < Delay)
                    await Clock.DelayAsync(Delay - elapsed, cancellationToken).ConfigureAwait(false);
            }
        }

        public void CancelDelays(TimeSpan? cancellationDelay = null)
        {
            var delay = Math.Max(0, (cancellationDelay ?? CancellationDelay).TotalSeconds);
            if (delay > 0) {
                Clock.DelayAsync(TimeSpan.FromSeconds(delay)).ContinueWith(_ => CancelDelays(TimeSpan.Zero));
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
}
