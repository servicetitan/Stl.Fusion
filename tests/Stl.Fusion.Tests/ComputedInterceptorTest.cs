using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class ComputedInterceptorTest(ITestOutputHelper @out) : FusionTestBase(@out)
{
    [Fact]
    public async Task AutoRecomputeTest()
    {
        var stateFactory = Services.StateFactory();
        var time = Services.GetRequiredService<ITimeService>();
        var c = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(1)));

        var count = 0L;
        using var state = stateFactory.NewComputed<DateTime>(
            FixedDelayer.Instant,
            async (_, ct) => await c.Use(ct));
        state.Updated += (s, _)
            => Log?.LogInformation($"{++count} -> {s.Value:hh:mm:ss:fff}");

        await TestExt.WhenMet(
            () => count.Should().BeGreaterThan(2),
            TimeSpan.FromSeconds(5));
        var lastCount = count;
        state.Dispose();

        await Task.Delay(1000);
        count.Should().Be(lastCount);
    }

    [Fact]
    public async Task CancellationTest()
    {
        var time = Services.GetRequiredService<ITimeService>();

        for (var i = 0; i < 5; i++) {
            await Task.Delay(300);
            var cts = new CancellationTokenSource();
            var task = time.GetTimeWithDelay(cts.Token);
            cts.Cancel();
            try {
                await task.ConfigureAwait(false);
            }
            catch {
                // Intended
            }
            task.IsCanceled.Should().BeTrue();

            task = time.GetTimeWithDelay(default);
            await TestExt.WhenMet(
                () => task.IsCompletedSuccessfully().Should().BeTrue(),
                TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task InvalidationAndCachingTest1()
    {
        var time = Services.GetRequiredService<ITimeService>();

        var c1 = await Computed.Capture(() => time.GetTime());

        // Wait for time invalidation
        await Task.Delay(500);

        var c2a = await Computed.Capture(() => time.GetTime());
        c2a.Should().NotBeSameAs(c1);
        var c2b = await Computed.Capture(() => time.GetTime());
        c2b.Should().BeSameAs(c2a);
    }

    [Fact]
    public async Task InvalidationAndCachingTest2()
    {
        // TODO: Fix the test so that it starts right after the time invalidation,
        // otherwise it has a tiny chance of failure
        var time = Services.GetRequiredService<ITimeService>();

        var c1 = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(1)));
        c1.Should().NotBeNull();
        var c2 = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(2)));
        c2.Should().NotBeNull();
        c1.Should().NotBeSameAs(c2);

        var c1a = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(1)));
        var c2a = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(2)));
        c1.Should().BeSameAs(c1a);
        c2.Should().BeSameAs(c2a);

        // Wait for time invalidation
        await Task.Delay(500);

        c1a = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(1)));
        c2a = await Computed.Capture(() => time.GetTimeWithOffset(TimeSpan.FromSeconds(2)));
        c1.Should().NotBeSameAs(c1a);
        c2.Should().NotBeSameAs(c2a);
    }
}
