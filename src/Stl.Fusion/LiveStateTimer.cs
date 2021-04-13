using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time;

namespace Stl.Fusion
{
    public interface ILiveStateTimer
    {
        Task UpdateDelay(int retryCount, CancellationToken cancellationToken = default);
        Task UserCausedUpdateDelay(IComputed computed, CancellationToken cancellationToken = default);
    }

    public record LiveStateTimer : ILiveStateTimer
    {
        public static LiveStateTimer Default { get; } = new();
        public static LiveStateTimer ZeroUpdateDelay { get; } = Default with {
            UpdateDelayDuration = default,
            UserCausedUpdateDelayDuration = default,
        };
        public static LiveStateTimer MinUpdateDelay { get; } = Default with {
            UpdateDelayDuration = Default.UserCausedUpdateDelayDuration,
        };

        public IMomentClock Clock { get; init; } = CpuClock.Instance;
        public TimeSpan UpdateDelayDuration { get; init; } = TimeSpan.FromSeconds(1);
        public TimeSpan MinErrorDelayDuration { get; init; } =  TimeSpan.FromSeconds(5);
        public TimeSpan MaxErrorDelayDuration { get; init; } = TimeSpan.FromMinutes(2);
        public TimeSpan UserCausedUpdateWaitDuration { get; init; } = TimeSpan.FromSeconds(2);
        public TimeSpan UserCausedUpdateDelayDuration { get; init; } = TimeSpan.FromMilliseconds(50);

        public virtual async Task UpdateDelay(int retryCount, CancellationToken cancellationToken = default)
        {
            var delay = Math.Max(0, UpdateDelayDuration.TotalSeconds);
            var start = Clock.Now;

            if (retryCount > 0) {
                var errorDelay = Math.Pow(Math.Sqrt(2), retryCount - 1) * MinErrorDelayDuration.TotalSeconds;
                errorDelay = Math.Min(MaxErrorDelayDuration.TotalSeconds, errorDelay);
                delay = errorDelay;
            }
            if (delay <= 0)
                return;

            await Clock.Delay(TimeSpan.FromSeconds(delay), cancellationToken)
                .ConfigureAwait(false);
            if (retryCount > 0) {
                // If it's an error, we still want to enforce at least
                // the default delay -- even if the delay was cancelled.
                var elapsed = Clock.Now - start;
                if (elapsed < UpdateDelayDuration)
                    await Clock.Delay(UpdateDelayDuration - elapsed, cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task UserCausedUpdateDelay(IComputed computed, CancellationToken cancellationToken = default)
        {
            if (!computed.IsInvalidated()) {
                var whenInvalidatedTask = computed.WhenInvalidated(cancellationToken);
                await whenInvalidatedTask
                    .WithTimeout(Clock, UserCausedUpdateWaitDuration, CancellationToken.None)
                    .ConfigureAwait(false);
                if (!whenInvalidatedTask.IsCompleted)
                    computed.Invalidate();
            }
            await Clock.Delay(UserCausedUpdateDelayDuration, cancellationToken).ConfigureAwait(false);
        }

        // We want referential equality back for this type:
        // it's a record solely to make it possible to use it with "with" keyword
        public virtual bool Equals(LiveStateTimer? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
