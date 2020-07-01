using System.ComponentModel;
using Stl.Locking;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Category(nameof(TimeSensitiveTests))]
    public class AsyncLockTest : AsyncLockTestBase
    {
        public AsyncLockTest(ITestOutputHelper @out) : base(@out) { }

        protected override IAsyncLock CreateAsyncLock(ReentryMode reentryMode) 
            => new AsyncLock(reentryMode);

        protected override void AssertResourcesReleased() 
        { }
    }
}
