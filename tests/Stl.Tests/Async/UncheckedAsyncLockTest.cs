using Stl.Locking;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class UncheckedAsyncLockTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task WaitTest1()
    {
        var l = new AsyncLock();
        l.ReentryMode.Should().Be(LockReentryMode.Unchecked);
        var r1 = await l.Lock().ConfigureAwait(false);
        r1.MarkLockedLocally();
        l.IsLockedLocally.Should().BeFalse();
        var r2Task = l.Lock();
        r1.Dispose();
        await r2Task.ConfigureAwait(false);
    }

    [Fact]
    public async Task WaitTest2()
    {
        var l = new AsyncLock();
        l.ReentryMode.Should().Be(LockReentryMode.Unchecked);
        var r1Task = l.Lock();
        var r2Task = l.Lock();
        var r1 = await r1Task.ConfigureAwait(false);
        r1.MarkLockedLocally();
        l.IsLockedLocally.Should().BeFalse();
        r1.Dispose();
        var r2 = await r2Task.ConfigureAwait(false);
        r2.Dispose();
    }
}
