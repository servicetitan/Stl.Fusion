using System;
using System.Threading.Tasks;
using Stl.Time;

namespace Stl.Fusion.UI
{
    public static class UICommandTrackerEx
    {
        public static Task<UICommandEvent> LastOrWhenCommandCompleted(this IUICommandTracker commandTracker,
            TimeSpan maxRecency,
            IMomentClock? clock = null)
        {
            clock ??= CpuClock.Instance;
            var cutoff = clock.Now - maxRecency;
            var lastCommand = commandTracker.LastCommandCompleted;
            return lastCommand?.CompletedAt >= cutoff
                ? Task.FromResult(lastCommand)
                : commandTracker.WhenCommandCompleted();
        }
    }
}
