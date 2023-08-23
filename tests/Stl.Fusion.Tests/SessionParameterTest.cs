using Stl.Fusion.Authentication;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class SessionParameterTest(ITestOutputHelper @out) : SimpleFusionTestBase(@out)
{
    [Fact]
    public async Task BasicTest()
    {
        using var stopCts = new CancellationTokenSource();
        var cancellationToken = stopCts.Token;

        async Task Watch<T>(string name, Computed<T> computed)
        {
            while (true) {
                Out.WriteLine($"{name}: {computed.Value}, {computed}");
                await computed.WhenInvalidated(cancellationToken);
                Out.WriteLine($"{name}: {computed.Value}, {computed}");
                computed = await computed.Update(cancellationToken);
            }
        }

        var services = CreateServicesWithComputeService<PerUserCounterService>();
        var counters = services.GetRequiredService<PerUserCounterService>();
        var sessionA = Session.New();
        var sessionB = Session.New();

        var x1 = await counters.Get("x", sessionA);
        await counters.Increment("x", sessionA);
        var x2 = await counters.Get("x", sessionA);
        x2.Should().Be(x1 + 1);

        var session = sessionA;
        var aaComputed = await Computed.Capture(() => counters.Get("a", session));
        _ = Task.Run(() => Watch(nameof(aaComputed), aaComputed));
        var abComputed = await Computed.Capture(() => counters.Get("b", session));
        _ = Task.Run(() => Watch(nameof(abComputed), abComputed));

        session = sessionB;
        var baComputed = await Computed.Capture(() => counters.Get("a", session));
        _ = Task.Run(() => Watch(nameof(baComputed), baComputed));

        session = sessionA;
        await counters.Increment("a", session);
        (await aaComputed.Update()).Value.Should().Be(1);
        (await abComputed.Update()).Value.Should().Be(0);
        (await baComputed.Update()).Value.Should().Be(0);
        await counters.Increment("b", session);
        (await aaComputed.Update()).Value.Should().Be(1);
        (await abComputed.Update()).Value.Should().Be(1);
        (await baComputed.Update()).Value.Should().Be(0);

        session = sessionB;
        await counters.Increment("a", session);
        (await aaComputed.Update()).Value.Should().Be(1);
        (await abComputed.Update()).Value.Should().Be(1);
        (await baComputed.Update()).Value.Should().Be(1);
        await counters.Increment("b", session);
        (await aaComputed.Update()).Value.Should().Be(1);
        (await abComputed.Update()).Value.Should().Be(1);
        (await baComputed.Update()).Value.Should().Be(1);

        stopCts.Cancel();
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddFusion().AddInMemoryAuthService();
    }
}
