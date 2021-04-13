using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time;

namespace Stl.Fusion
{
    public interface IUpdateDelayer
    {
        Task UpdateDelay(IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default);
        Task UserCausedUpdateDelay(IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default);
    }

    public record UpdateDelayer : IUpdateDelayer
    {
        public static UpdateDelayer Default { get; } = new();
        public static UpdateDelayer ZeroUpdateDelay { get; } = new(0, 0);
        public static UpdateDelayer MinUpdateDelay { get; } = new(Default.UserCausedUpdateDelayDuration);

        public IMomentClock Clock { get; init; } = CpuClock.Instance;
        public TimeSpan UpdateDelayDuration { get; init; } = TimeSpan.FromSeconds(1);
        public TimeSpan MinRetryDelayDuration { get; init; } =  TimeSpan.FromSeconds(5);
        public TimeSpan MaxRetryDelayDuration { get; init; } = TimeSpan.FromMinutes(2);
        public TimeSpan UserCausedUpdateWaitDuration { get; init; } = TimeSpan.FromSeconds(2);
        public TimeSpan UserCausedUpdateDelayDuration { get; init; } = TimeSpan.FromMilliseconds(50);

        public UpdateDelayer() { }
        public UpdateDelayer(double updateDelaySeconds)
            : this(TimeSpan.FromSeconds(updateDelaySeconds)) { }
        public UpdateDelayer(double updateDelaySeconds, double userCausedUpdateDelaySeconds)
            : this(TimeSpan.FromSeconds(updateDelaySeconds), TimeSpan.FromSeconds(userCausedUpdateDelaySeconds)) { }
        public UpdateDelayer(TimeSpan updateDelayDuration)
            => UpdateDelayDuration = updateDelayDuration;
        public UpdateDelayer(TimeSpan updateDelayDuration, TimeSpan userCausedUpdateDelayDuration)
        {
            UpdateDelayDuration = updateDelayDuration;
            UserCausedUpdateDelayDuration = userCausedUpdateDelayDuration;
        }

        public virtual async Task UpdateDelay(
            IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default)
        {
            var start = Clock.Now;
            var retryCount = stateSnapshot.RetryCount;
            var delay = retryCount > 0
                ? GetRetryDelay(retryCount)
                : TimeSpanEx.Max(TimeSpan.Zero, UpdateDelayDuration);

            await stateSnapshot.WhenUpdated()
                .WithTimeout(Clock, delay, cancellationToken)
                .ConfigureAwait(false);
            if (stateSnapshot.WhenUpdated().IsCompleted || retryCount <= 0)
                return;

            // If we're retrying, we still want to enforce at least
            // the default delay -- even if the delay was cancelled.
            var elapsed = Clock.Now - start;
            if (elapsed < UpdateDelayDuration)
                await Clock.Delay(UpdateDelayDuration - elapsed, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task UserCausedUpdateDelay(
            IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default)
        {
            var computed = stateSnapshot.Computed;
            if (!computed.IsInvalidated()) {
                await computed.WhenInvalidated(cancellationToken)
                    .WithTimeout(Clock, UserCausedUpdateWaitDuration, cancellationToken)
                    .ConfigureAwait(false);
                if (!computed.IsInvalidated())
                    return; // Nothing came through yet, so no reason to update
            }
            await stateSnapshot.WhenUpdated()
                .WithTimeout(Clock, UserCausedUpdateDelayDuration, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual TimeSpan GetRetryDelay(int retryCount)
        {
            var delay = Math.Pow(Math.Sqrt(2), retryCount - 1) * MinRetryDelayDuration.TotalSeconds;
            delay = Math.Min(MaxRetryDelayDuration.TotalSeconds, delay);
            return TimeSpanEx.Max(TimeSpan.Zero, TimeSpan.FromSeconds(delay));
        }

        // We want referential equality back for this type:
        // it's a record solely to make it possible to use it with "with" keyword
        public virtual bool Equals(UpdateDelayer? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
