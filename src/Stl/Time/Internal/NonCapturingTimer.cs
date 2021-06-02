using System;
using System.Threading;

namespace Stl.Time.Internal
{
    public static class NonCapturingTimer
    {
        public static Timer Create(
            TimerCallback callback,
            object state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            var isFlowSuppressed = false;
            try {
                if (!ExecutionContext.IsFlowSuppressed()) {
                    ExecutionContext.SuppressFlow();
                    isFlowSuppressed = true;
                }
                return new Timer(callback, state, dueTime, period);
            }
            finally {
                if (isFlowSuppressed)
                    ExecutionContext.RestoreFlow();
            }
        }
    }
}
