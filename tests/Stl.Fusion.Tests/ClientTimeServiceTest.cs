using Stl.Fusion.Bridge;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class ClientTimeServiceTest : FusionTestBase
{
    public ClientTimeServiceTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

    private TimeSpan GetEpsilon()
    {
#if NETCOREAPP
        var epsilon = TimeSpan.FromSeconds(0.5);
#else
        var epsilon = TimeSpan.FromSeconds(0.7);
#endif
        return epsilon;
    }

    [Fact]
    public async Task Test1()
    {
        var epsilon = GetEpsilon();

        await using var serving = await WebHost.Serve();
        var client = ClientServices.GetRequiredService<IClientTimeService>();
        var cTime = await Computed.Capture(() => client.GetTime());

        cTime.Options.AutoInvalidationDelay.Should().Be(ComputedOptions.Default.AutoInvalidationDelay);
        if (!cTime.IsConsistent()) {
            cTime = await cTime.Update();
            cTime.IsConsistent().Should().BeTrue();
        }
        (DateTime.Now - cTime.Value).Should().BeLessThan(epsilon);

        await TestExt.WhenMet(
            () => cTime.IsConsistent().Should().BeFalse(),
            TimeSpan.FromSeconds(5));
        var time = await cTime.Use();
        (DateTime.Now - time).Should().BeLessThan(epsilon);
    }

    [Fact]
    public async Task Test2()
    {
        var epsilon = GetEpsilon();
        if (TestRunnerInfo.IsBuildAgent())
            epsilon = epsilon.Multiply(2);

        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IClientTimeService>();

        for (int i = 0; i < 20; i++) {
            var time = await service.GetTime();
            (DateTime.Now - time).Should().BeLessThan(epsilon);
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
    }

    [Fact]
    public async Task TestFormattedTime()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IClientTimeService>();

        (await service.GetFormattedTime("")).Should().Be("");
        (await service.GetFormattedTime("null")).Should().Be("");

        var format = "{0}";
        var matchCount = 0;
        for (int i = 0; i < 20; i++) {
            var time = await service.GetTime();
            var formatted = await service.GetFormattedTime(format);
            var expected = string.Format(format, time);
            if (formatted == expected)
                matchCount++;
            await Task.Delay(TimeSpan.FromSeconds(0.1));
        }
        matchCount.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task TestNoControllerMethod()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IClientTimeService>();

        await Assert.ThrowsAsync<ReplicaException>(async () => {
            await service.GetTimeNoControllerMethod();
        });
    }

    [Fact]
    public async Task TestNoPublication()
    {
        await using var serving = await WebHost.Serve();
        var service = ClientServices.GetRequiredService<IClientTimeService>();

        await Assert.ThrowsAsync<ReplicaException>(async () => {
            await service.GetTimeNoPublication();
        });
    }
}
