using System.Threading;
using Stl.Internal;

namespace Stl.Locking.Internal
{
    public class ReentryCounter
    {
        private volatile int _count;

        public int Count => _count;

        public ReentryCounter() { }
        public ReentryCounter(int initialCount)
        {
            _count = initialCount;
        }

        public int Enter(ReentryMode reentryMode)
        {
            if (reentryMode != ReentryMode.CheckedFail)
                return Interlocked.Increment(ref _count);
            if (0 != Interlocked.CompareExchange(ref _count, 1, 0))
                throw Errors.AlreadyLocked();
            return 1;
        }

        public bool TryReenter(ReentryMode reentryMode)
        {
            if (reentryMode == ReentryMode.CheckedFail) {
                if (Count > 0)
                    throw Errors.AlreadyLocked();
                return false;
            }

            var spinWait = new SpinWait();
            var count = Count;
            while (count > 0) {
                var oldCount = Interlocked.CompareExchange(ref _count, count + 1, count);
                if (oldCount == count)
                    return true;
                count = oldCount;
                spinWait.SpinOnce();
            }
            return false;
        }

        public int Leave()
            => Interlocked.Decrement(ref _count);
    }
}
