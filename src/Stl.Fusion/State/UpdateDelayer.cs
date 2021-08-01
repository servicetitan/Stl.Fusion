using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.UI;
using Stl.Time;

namespace Stl.Fusion
{
    public interface IUpdateDelayer
    {
        Task UpdateDelay(IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default);
    }

    public record UpdateDelayer : IUpdateDelayer
    {
        public static class Defaults
        {
            public static TimeSpan UpdateDelayDuration { get; set; } = TimeSpan.FromSeconds(1);
            public static TimeSpan MinRetryDelayDuration { get; set; } =   TimeSpan.FromSeconds(2);
            public static TimeSpan MaxRetryDelayDuration { get; set; } =  TimeSpan.FromMinutes(2);
            public static TimeSpan UICommandRecencyDelta { get; set; } =  TimeSpan.FromMilliseconds(100);
            public static TimeSpan UICommandUpdateDelayDuration { get; set; } =  TimeSpan.FromMilliseconds(50);
        }

        public static UpdateDelayer ZeroDelay { get; } = new(UICommandTracker.None, 0, 0);
        public static UpdateDelayer MinDelay { get; } = new(UICommandTracker.None, Defaults.UICommandUpdateDelayDuration);

        public IUICommandTracker CommandTracker { get; init; }
        public TimeSpan UpdateDelayDuration { get; init; } = Defaults.UpdateDelayDuration;
        public TimeSpan MinRetryDelayDuration { get; init; } =  Defaults.MinRetryDelayDuration;
        public TimeSpan MaxRetryDelayDuration { get; init; } = Defaults.MaxRetryDelayDuration;
        public TimeSpan UICommandRecencyDelta { get; init; } = Defaults.UICommandRecencyDelta;
        public TimeSpan UICommandUpdateDelayDuration { get; init; } = Defaults.UICommandUpdateDelayDuration;
        public IMomentClock Clock => CommandTracker.Clock;

        public UpdateDelayer(IUICommandTracker commandTracker)
            => CommandTracker = commandTracker;

        public UpdateDelayer(IUICommandTracker commandTracker, double updateDelaySeconds)
            : this(commandTracker, TimeSpan.FromSeconds(updateDelaySeconds)) { }

        public UpdateDelayer(IUICommandTracker commandTracker, TimeSpan updateDelayDuration)
        {
            CommandTracker = commandTracker;
            UpdateDelayDuration = updateDelayDuration;
        }

        public UpdateDelayer(
            IUICommandTracker commandTracker,
            double updateDelaySeconds,
            double uiCommandUpdateDelaySeconds)
            : this(commandTracker,
                TimeSpan.FromSeconds(updateDelaySeconds),
                TimeSpan.FromSeconds(uiCommandUpdateDelaySeconds)) { }

        public UpdateDelayer(
            IUICommandTracker commandTracker,
            TimeSpan updateDelayDuration,
            TimeSpan uiCommandUpdateDelayDuration)
        {
            CommandTracker = commandTracker;
            UpdateDelayDuration = updateDelayDuration;
            UICommandUpdateDelayDuration = uiCommandUpdateDelayDuration;
        }

        public virtual async Task UpdateDelay(
            IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default)
        {
            var computed = stateSnapshot.Computed;
            using var doneCts = new CancellationTokenSource();
            var doneCancellationToken = doneCts.Token;
            // ReSharper disable once AccessToDisposedClosure
            using var _ = cancellationToken.Register(() => doneCts.Cancel());
            try {
                var whenInvalidatedTask = computed.WhenInvalidated(doneCancellationToken);
                var whenUpdatedTask = stateSnapshot.WhenUpdated();

                // 1. Wait for invalidation
                await whenInvalidatedTask.ConfigureAwait(false);
                if (whenUpdatedTask.IsCompleted)
                    return;

                // 2. Wait a bit to see if the invalidation is caused by UI command
                var delayStart = Clock.Now;
                var commandCompletedTask = CommandTracker.LastOrWhenCommandCompleted(UICommandRecencyDelta);
                if (UpdateDelayDuration > TimeSpan.Zero) {
                    if (!commandCompletedTask.IsCompleted) {
                        var waitDuration = TimeSpanEx.Min(UpdateDelayDuration, UICommandRecencyDelta);
                        await Task.WhenAny(whenUpdatedTask, commandCompletedTask)
                            .WithTimeout(Clock, waitDuration, cancellationToken)
                            .ConfigureAwait(false);
                        if (whenUpdatedTask.IsCompleted)
                            return;
                    }
                }

                // 3. Actual delay
                var retryCount = stateSnapshot.RetryCount;
                var retryDelay = GetUpdateDelay(commandCompletedTask.IsCompleted, retryCount);
                var remainingDelay = delayStart + retryDelay - Clock.Now;
                if (remainingDelay < TimeSpan.Zero)
                    return;
                await whenUpdatedTask
                    .WithTimeout(Clock, remainingDelay, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally {
                doneCts.Cancel();
            }
        }

        public virtual TimeSpan GetUpdateDelay(bool isUICommandCaused, int retryCount)
        {
            var uiCommandUpdateDelayDuration = TimeSpanEx.Min(UpdateDelayDuration, UICommandUpdateDelayDuration);
            var baseDelay = isUICommandCaused ? uiCommandUpdateDelayDuration : UpdateDelayDuration;
            if (retryCount <= 0)
                return baseDelay;
            var retryDelay = Math.Pow(Math.Sqrt(2), retryCount) * MinRetryDelayDuration.TotalSeconds;
            retryDelay = Math.Min(MaxRetryDelayDuration.TotalSeconds, retryDelay);
            retryDelay = Math.Max(MinRetryDelayDuration.TotalSeconds, retryDelay);
            return TimeSpan.FromSeconds(retryDelay);
        }

        // We want referential equality back for this type:
        // it's a record solely to make it possible to use it with "with" keyword
        public virtual bool Equals(UpdateDelayer? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
