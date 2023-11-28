using Stl.Locking;

namespace Stl.Tests.Async;

public class ReentrantAsyncLockTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task CheckedPassTest()
    {
        var l = new AsyncLock(LockReentryMode.CheckedPass);
        var task1 = await Run().ConfigureAwait(false);
        (await task1.ConfigureAwait(false)).Should().BeTrue();

        async Task<Task<bool>> Run() {
            using var r1 = await l.Lock().ConfigureAwait(false);
            r1.MarkLockedLocally();
            using var r2 = await l.Lock().ConfigureAwait(false);
            r2.MarkLockedLocally();
            (await Task.Run(async () => {
                using var r3 = await l.Lock().ConfigureAwait(false);
                r3.MarkLockedLocally();
                return true;
            })).Should().BeTrue();
            l.IsLockedLocally = false;
            return Task.Run(async () => {
                using var r4 = await l.Lock().ConfigureAwait(false);
                r4.MarkLockedLocally();
                return true;
            });
        }
    }

    [Fact]
    public async Task CheckedFailTest()
    {
        var l = new AsyncLock(LockReentryMode.CheckedFail);
        var task1 = await Run().ConfigureAwait(false);
        (await task1.ConfigureAwait(false)).Should().BeTrue();

        async Task<Task<bool>> Run() {
            using var r1 = await l.Lock();
            r1.MarkLockedLocally();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await l.Lock();
            });
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Task.Run(async () => {
                    using var _ = await l.Lock().ConfigureAwait(false);
                    return true;
                });
            });
            l.IsLockedLocally = false;
            return Task.Run(async () => {
                using var r2 = await l.Lock().ConfigureAwait(false);
                r2.MarkLockedLocally();
                return true;
            });
        }

    }
}
