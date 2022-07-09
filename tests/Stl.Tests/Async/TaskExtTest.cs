using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class TaskExtTest : TestBase
{
    public TaskExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task ToResultTest()
    {
        using var cts = new CancellationTokenSource(200);
        var t1 = Task.Delay(50);
        var t2 = IntDelay(50);
        var t3 = FailDelay(50);
        var t4 = FailIntDelay(50);
        var t5 = Task.Delay(500).WaitAsync(cts.Token);
        var t6 = IntDelay(500).WaitAsync(cts.Token);

        Assert.Throws<InvalidOperationException>(() => t1.ToResultSynchronously());
        Assert.Throws<InvalidOperationException>(() => t2.ToResultSynchronously());
        Assert.Throws<InvalidOperationException>(() => t3.ToResultSynchronously());
        Assert.Throws<InvalidOperationException>(() => t4.ToResultSynchronously());
        Assert.Throws<InvalidOperationException>(() => t5.ToResultSynchronously());
        Assert.Throws<InvalidOperationException>(() => t6.ToResultSynchronously());

        (await t1.ToResultAsync()).HasValue.Should().BeTrue();
        (await t2.ToResultAsync()).Value.Should().Be(1);
        (await t3.ToResultAsync()).Error.Should().BeOfType<InvalidOperationException>();
        (await t4.ToResultAsync()).Error.Should().BeOfType<InvalidOperationException>();
        (await t5.ToResultAsync()).Error.Should().BeAssignableTo<OperationCanceledException>();
        (await t6.ToResultAsync()).Error.Should().BeAssignableTo<OperationCanceledException>();

        t1.ToResultSynchronously().HasValue.Should().BeTrue();
        t2.ToResultSynchronously().Value.Should().Be(1);
        t3.ToResultSynchronously().Error.Should().BeOfType<InvalidOperationException>();
        t4.ToResultSynchronously().Error.Should().BeOfType<InvalidOperationException>();
        t5.ToResultSynchronously().Error.Should().BeAssignableTo<OperationCanceledException>();
        t6.ToResultSynchronously().Error.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitAsyncTest1()
    {
        using var cts = new CancellationTokenSource(100);
        var t0 = Task.Delay(50).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = Task.Delay(500).WaitAsync(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = Task.Delay(500).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t3 = FailDelay(10).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);

        await t0;
        await Assert.ThrowsAnyAsync<TimeoutException>(() => t1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t2);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => t3);
    }

    [Fact]
    public async Task WaitAsyncTest2()
    {
        using var cts = new CancellationTokenSource(100);

        var t0 = IntDelay(50).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = IntDelay(500).WaitAsync(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = IntDelay(500).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t3 = FailIntDelay(10).WaitAsync(TimeSpan.FromMilliseconds(200), cts.Token);

        (await t0).Should().Be(1);
        await Assert.ThrowsAnyAsync<TimeoutException>(() => t1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t2);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => t3);
    }

    [Fact]
    public async Task WaitResultAsyncTest1()
    {
        using var cts = new CancellationTokenSource(100);
        var t0 = Task.Delay(50).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = Task.Delay(500).WaitResultAsync(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = Task.Delay(500).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t3 = FailDelay(10).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);

        (await t0).HasValue.Should().BeTrue();
        (await t1).HasValue.Should().BeFalse();
        (await t2).Error.Should().BeAssignableTo<OperationCanceledException>();
        (await t3).Error.Should().BeAssignableTo<InvalidOperationException>();
    }

    [Fact]
    public async Task WaitResultAsyncTest2()
    {
        using var cts = new CancellationTokenSource(100);

        var t0 = IntDelay(50).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t1 = IntDelay(500).WaitResultAsync(TimeSpan.FromMilliseconds(50), cts.Token);
        var t2 = IntDelay(500).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);
        var t3 = FailIntDelay(10).WaitResultAsync(TimeSpan.FromMilliseconds(200), cts.Token);

        (await t0).Value.Should().Be(1);
        (await t1).HasValue.Should().BeFalse();
        (await t2).Error.Should().BeAssignableTo<OperationCanceledException>();
        (await t3).Error.Should().BeAssignableTo<InvalidOperationException>();
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
