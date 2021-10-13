using System;
using System.Threading.Tasks;

namespace Stl.Fusion.UI
{
    public static class UICommandTrackerExt
    {
        public static Task<UICommandEvent> LastOrWhenCommandCompleted(
            this IUICommandTracker uiCommandTracker,
            TimeSpan maxRecency)
        {
            var cutoff = uiCommandTracker.Clocks.UIClock.Now - maxRecency;
            var lastCommand = uiCommandTracker.LastCommandCompleted;
            return lastCommand?.CompletedAt >= cutoff
                ? Task.FromResult(lastCommand)
                : uiCommandTracker.WhenCommandCompleted();
        }
    }
}
