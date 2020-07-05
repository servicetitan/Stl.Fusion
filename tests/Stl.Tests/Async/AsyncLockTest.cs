using Stl.Locking;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Async
{
    [Collection(nameof(TimeSensitive)), Trait("Category", nameof(TimeSensitive))]
    public class AsyncLockTest : AsyncLockTestBase
    {
        public AsyncLockTest(ITestOutputHelper @out) : base(@out) { }

        protected override IAsyncLock CreateAsyncLock(ReentryMode reentryMode) 
            => new AsyncLock(reentryMode);

        protected override void AssertResourcesReleased() 
        { }
    }
}
