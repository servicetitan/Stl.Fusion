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

        public static UpdateDelayer ZeroDelay { get; } = new(UI.UICommandTracker.None, 0, 0);
        public static UpdateDelayer MinDelay { get; } = new(UI.UICommandTracker.None, Defaults.UICommandUpdateDelayDuration);

        public IUICommandTracker UICommandTracker { get; init; }
        public MomentClockSet Clocks => UICommandTracker.Clocks;
        public TimeSpan UpdateDelayDuration { get; init; } = Defaults.UpdateDelayDuration;
        public TimeSpan MinRetryDelayDuration { get; init; } =  Defaults.MinRetryDelayDuration;
        public TimeSpan MaxRetryDelayDuration { get; init; } = Defaults.MaxRetryDelayDuration;
        public TimeSpan UICommandRecencyDelta { get; init; } = Defaults.UICommandRecencyDelta;
        public TimeSpan UICommandUpdateDelayDuration { get; init; } = Defaults.UICommandUpdateDelayDuration;

        public UpdateDelayer(IUICommandTracker uiCommandTracker)
            => UICommandTracker = uiCommandTracker;

        public UpdateDelayer(IUICommandTracker uiCommandTracker, double updateDelaySeconds)
            : this(uiCommandTracker, TimeSpan.FromSeconds(updateDelaySeconds)) { }

        public UpdateDelayer(IUICommandTracker uiCommandTracker, TimeSpan updateDelayDuration)
        {
            UICommandTracker = uiCommandTracker;
            UpdateDelayDuration = updateDelayDuration;
        }

        public UpdateDelayer(
            IUICommandTracker uiCommandTracker,
            double updateDelaySeconds,
            double uiCommandUpdateDelaySeconds)
            : this(uiCommandTracker,
                TimeSpan.FromSeconds(updateDelaySeconds),
                TimeSpan.FromSeconds(uiCommandUpdateDelaySeconds)) { }

        public UpdateDelayer(
            IUICommandTracker uiCommandTracker,
            TimeSpan updateDelayDuration,
            TimeSpan uiCommandUpdateDelayDuration)
        {
            UICommandTracker = uiCommandTracker;
            UpdateDelayDuration = updateDelayDuration;
            UICommandUpdateDelayDuration = uiCommandUpdateDelayDuration;
        }

        public virtual async Task UpdateDelay(
            IStateSnapshot stateSnapshot, CancellationToken cancellationToken = default)
        {
            // 1. The update already happened? No need for delay.
            var whenUpdatedTask = stateSnapshot.WhenUpdated();
            if (whenUpdatedTask.IsCompleted)
                return;

            // 2. Wait a bit to see if the invalidation is caused by a UI command
            var delayStart = Clocks.UIClock.Now;
            var commandCompletedTask = UICommandTracker.LastOrWhenCommandCompleted(UICommandRecencyDelta);
            if (UpdateDelayDuration > TimeSpan.Zero) {
                if (!commandCompletedTask.IsCompleted) {
                    var waitDuration = TimeSpanExt.Min(UpdateDelayDuration, UICommandRecencyDelta);
                    await Task.WhenAny(whenUpdatedTask, commandCompletedTask)
                        .WithTimeout(Clocks.UIClock, waitDuration, cancellationToken)
                        .ConfigureAwait(false);
                    if (whenUpdatedTask.IsCompleted)
                        return;
                }
            }

            // 3. Actual delay
            var retryCount = stateSnapshot.RetryCount;
            var retryDelay = GetUpdateDelay(commandCompletedTask.IsCompleted, retryCount);
            var remainingDelay = delayStart + retryDelay - Clocks.UIClock.Now;
            if (remainingDelay < TimeSpan.Zero)
                return;
            await whenUpdatedTask
                .WithTimeout(Clocks.UIClock, remainingDelay, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual TimeSpan GetUpdateDelay(bool isUICommandCaused, int retryCount)
        {
            var uiCommandUpdateDelayDuration = TimeSpanExt.Min(UpdateDelayDuration, UICommandUpdateDelayDuration);
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
