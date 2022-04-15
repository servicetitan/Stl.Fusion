using Stl.Fusion.Authentication;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class SessionParameterTest : SimpleFusionTestBase
{
    public SessionParameterTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services)
        => services.AddFusion().AddAuthentication();

    private static object syncObject = new Object();

    [Fact]
    public async Task BasicTest()
    {
        using var stopCts = new CancellationTokenSource();
        var cancellationToken = stopCts.Token;

        async Task Watch<T>(string name, IComputed<T> computed)
        {
            while (true) {
                Out.WriteLine($"{name}: {computed.Value}, {computed}");
                await computed.WhenInvalidated(cancellationToken);
                Out.WriteLine($"{name}: {computed.Value}, {computed}");
                computed = await computed.Update(cancellationToken);
            }
        }

        var services = CreateServiceProviderFor<PerUserCounterService>();
        var counters = services.GetRequiredService<PerUserCounterService>();
        var sessionFactory = services.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();

        var session = sessionA;
        var aaComputed = await Computed.Capture(_ => counters.Get("a", session));
        _ = Task.Run(() => Watch(nameof(aaComputed), aaComputed));
        var abComputed = await Computed.Capture(_ => counters.Get("b", session));
        _ = Task.Run(() => Watch(nameof(abComputed), abComputed));

        session = sessionB;
        var baComputed = await Computed.Capture(_ => counters.Get("a", session));
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
}
