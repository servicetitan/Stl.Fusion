using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class TaskExtTest : TestBase
{
    public TaskExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task WithTimeoutTest1()
    {
        using var cts = new CancellationTokenSource(100);
        var t0 = Task.Delay(50).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = Task.Delay(500).WithTimeout(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = Task.Delay(500).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);
        var t4 = FailDelay(10).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);

        (await t0).Should().BeTrue();
        (await t1).Should().BeFalse();
        await t2.SuppressCancellation();
        t2.IsCanceled.Should().BeTrue();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await t4;
        });
    }

    [Fact]
    public async Task WithTimeoutTest2()
    {
        using var cts = new CancellationTokenSource(100);
        var t0 = IntDelay(50).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = IntDelay(500).WithTimeout(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = IntDelay(500).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);
        var t4 = FailIntDelay(10).WithTimeout(TimeSpan.FromMilliseconds(200), cts.Token);

        (await t0).Value.Should().Be(1);
        (await t1).HasValue.Should().BeFalse();
        await t2.SuppressCancellation();
        t2.IsCanceled.Should().BeTrue();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await t4;
        });
    }

    async Task FailDelay(int delay)
    {
        await Task.Delay(delay);
        throw new InvalidOperationException();
    }

    async Task<int> IntDelay(int delay)
    {
        await Task.Delay(delay);
        return 1;
    }

    async Task<int> FailIntDelay(int delay)
    {
        await Task.Delay(delay);
        throw new InvalidOperationException();
    }
}
