using System;
using System.Threading.Tasks;
using Stl.Time;

namespace Stl.Fusion.UI
{
    public static class UICommandTrackerEx
    {
        public static Task<UICommandEvent> LastOrWhenCommandCompleted(this IUICommandTracker uiCommandTracker,
            TimeSpan maxRecency,
            IMomentClock? clock = null)
        {
            clock ??= CpuClock.Instance;
            var cutoff = clock.Now - maxRecency;
            var lastCommand = uiCommandTracker.LastCommandCompleted;
            return lastCommand?.CompletedAt >= cutoff
                ? Task.FromResult(lastCommand)
                : uiCommandTracker.WhenCommandCompleted();
        }
    }
}
