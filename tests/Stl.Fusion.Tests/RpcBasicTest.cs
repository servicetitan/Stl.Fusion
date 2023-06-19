using Stl.Fusion.Extensions;
using Stl.Fusion.Tests.Services;
using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcBasicTest : SimpleFusionTestBase
{
    public RpcBasicTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServices();
        var counters = services.GetRequiredService<ICounterService>();

        var c = Computed.GetExisting(() => counters.Get("a"));
        c.Should().BeNull();

        c = await Computed.Capture(() => counters.Get("a"));
        c.Value.Should().Be(0);
        var c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().BeSameAs(c);

        await counters.Increment("a");
        await TestExt.WhenMet(
            () => c.IsConsistent().Should().BeFalse(),
            TimeSpan.FromSeconds(1));
        c1 = Computed.GetExisting(() => counters.Get("a"));
        c1.Should().NotBeNull();
    }

    [Fact]
    public async Task ConnectionMonitorTest()
    {
        var services = CreateServices();
        var testClient = services.GetRequiredService<RpcTestClient>();
        var clientPeer = testClient.Single().ClientPeer;
        var connectionMonitor = new RpcPeerConnectionMonitor(services);
        connectionMonitor.Start();

        await connectionMonitor.IsConnected.When(x => x)
            .WaitAsync(TimeSpan.FromSeconds(1));

        clientPeer.Disconnect(new InvalidOperationException("Disconnected!"));
        try {
            await connectionMonitor.IsConnected.When(x => !x)
                .WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (InvalidOperationException) {
            // It's our own one
        }

        await testClient[clientPeer].Connect();
        await connectionMonitor.IsConnected.When(x => x)
            .WaitAsync(TimeSpan.FromSeconds(1));
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var fusion = services.AddFusion();
        fusion.AddService<ICounterService, CounterService>(RpcServiceMode.RoutingServer);
    }
}
