using Stl.Fusion.Extensions;
using Stl.Fusion.Tests.Services;
using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class FusionRpcBasicTest(ITestOutputHelper @out) : SimpleFusionTestBase(@out)
{
    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var fusion = services.AddFusion();
        fusion.AddService<ICounterService, CounterService>(RpcServiceMode.RoutingServer);
    }

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
        c1.Should().BeNull();
    }

    [Fact]
    public async Task PeerMonitorTest()
    {
        var services = CreateServices();
        var testClient = services.GetRequiredService<RpcTestClient>();
        var clientPeer = testClient.Connections.First().Value.ClientPeer;
        var monitor = new RpcPeerStateMonitor(services, clientPeer);
        var state = monitor.State;
        await state.When(x => x.IsConnected).WaitAsync(TimeSpan.FromSeconds(1));

        _ = clientPeer.Disconnect(false, new InvalidOperationException("Disconnected!"));
        await state.When(x => x.Kind == RpcPeerStateKind.JustDisconnected)
            .WaitAsync(TimeSpan.FromSeconds(2));
        await state.When(x => !x.LikelyConnected).WaitAsync(TimeSpan.FromSeconds(5));

        await testClient[clientPeer].Connect();
        await state.When(x => x.IsConnected).WaitAsync(TimeSpan.FromSeconds(1));
    }
}
