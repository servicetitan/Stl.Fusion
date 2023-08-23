using Stl.Locking;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class AsyncLockTest(ITestOutputHelper @out) : AsyncLockTestBase(@out)
{
    protected override AsyncLock CreateAsyncLock(LockReentryMode reentryMode)
        => AsyncLock.New(reentryMode);

    protected override void AssertResourcesReleased()
    { }
}
