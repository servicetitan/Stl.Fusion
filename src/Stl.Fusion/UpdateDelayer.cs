using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.Time;

namespace Stl.Fusion
{
    public interface IUpdateDelayer
    {
        // DelayAsync should never throw OperationCancelledException, i.e.
        // the only result of cancellation there should be immediate completion.
        Task DelayAsync(IState state, CancellationToken cancellationToken = default);
        Task DelayAsync(int retryCount, CancellationToken cancellationToken = default);
        void CancelDelays(TimeSpan? postCancellationDelay = null);
    }

    public interface IUpdateDelayer<T> : IUpdateDelayer { }

    public class UpdateDelayer : IUpdateDelayer
    {
        public class Options : IOptions
        {
            public static Options InstantUpdates => new Options() {
                Delay = TimeSpan.Zero,
                MinExtraErrorDelay = TimeSpan.FromSeconds(1),
                DefaultPostCancellationDelay = TimeSpan.Zero,
            };

            public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MinExtraErrorDelay { get; set; } =  TimeSpan.FromSeconds(5);
            public TimeSpan MaxExtraErrorDelay { get; set; } = TimeSpan.FromMinutes(2);
            public TimeSpan DefaultPostCancellationDelay { get; set; } = TimeSpan.FromSeconds(0.05);
            public IMomentClock Clock { get; set; } = CoarseCpuClock.Instance;
        }

        private volatile Task<TimeSpan> _cancelDelaysTask = null!;
        protected Task<TimeSpan> CancelDelaysTask => _cancelDelaysTask;
        protected IMomentClock Clock { get; }

        public TimeSpan Delay { get; set; }
        public TimeSpan MinExtraErrorDelay { get; set; }
        public TimeSpan MaxExtraErrorDelay { get; set; }
        public TimeSpan DefaultPostCancellationDelay { get; set; }

        public UpdateDelayer(Options? options = null)
        {
            options = options.OrDefault();
            Delay = options.Delay;
            MinExtraErrorDelay = options.MinExtraErrorDelay;
            MaxExtraErrorDelay = options.MaxExtraErrorDelay;
            DefaultPostCancellationDelay = options.DefaultPostCancellationDelay;
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
            if (delay < 0)
                return;
            try {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                await Task.WhenAny(delayTask, CancelDelaysTask).ConfigureAwait(false);
                if (CancelDelaysTask.IsCompletedSuccessfully) {
                    var postCancellationDelay = CancelDelaysTask.Result;
                    if (postCancellationDelay > TimeSpan.Zero)
                        await Task.Delay(postCancellationDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                // This method should never throw OperationCanceledException
            }

            if (retryCount > 0) {
                // If it's an error, we still want to enforce at least
                // the default delay -- even if the delay was cancelled.
                var elapsed = Clock.Now - start;
                if (elapsed < Delay) {
                    try {
                        await Clock.DelayAsync(Delay - elapsed, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        // This method should never throw OperationCanceledException
                    }
                }
            }
        }

        public void CancelDelays(TimeSpan? postCancellationDelay = null)
        {
            var newTask = TaskSource.New<TimeSpan>(true).Task;
            var oldTask = Interlocked.Exchange(ref _cancelDelaysTask, newTask);
            if (oldTask != null)
                TaskSource.For(oldTask).SetResult(postCancellationDelay ?? DefaultPostCancellationDelay);
        }
    }

    public class UpdateDelayer<T> : UpdateDelayer, IUpdateDelayer<T>
    {
        public new class Options : UpdateDelayer.Options { }

        public UpdateDelayer(Options? options = null) : base(options) { }
    }
}
