using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Tests.Rpc;

public class RpcBasicTest : TestBase
{
    public RpcBasicTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task CallTest()
    {
        var services = CreateServerServices();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        var sum = await client.Sum(1, 2);
        sum.Should().Be(3);
        sum = await client.Sum(1, 2);
        sum.Should().Be(3);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(100_000)]
    public async Task PerformanceTest(int iterationCount)
    {
        var services = CreateServerServices();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();

        var startedAt = CpuTimestamp.Now;
        for (var i = iterationCount; i > 0; i--)
            await client.Sum(1, 2);
        var elapsed = startedAt.Elapsed;
        Out.WriteLine($"{iterationCount}: {iterationCount / elapsed.TotalSeconds:F} ops/s");
    }

    private IServiceProvider CreateServerServices()
    {
        var services = new ServiceCollection();
        var channelPair = CreateRpcChannelPair();
        services.AddSingleton(channelPair);
        services.AddSingleton<SimpleRpcService>();

        var rpc = services.AddRpc();
        var serverPeerName = new Symbol("server");
        rpc.HasPeerFactory(c => {
            var hub = c.RpcHub();
            return name => new RpcPeer(hub, name) {
                LocalServiceFilter = name == serverPeerName
                    ? _ => true
                    : _ => false
            };
        });
        rpc.HasPeerConnector((peer, _) => {
            var c = peer.Hub.Services;
            var cp = c.GetRequiredService<ChannelPair<RpcMessage>>();
            var channel = peer.Name == serverPeerName ? cp.Channel1 : cp.Channel2;
            return Task.FromResult(channel);
        });
        rpc.HasService<ISimpleRpcService>()
            .WithServer<SimpleRpcService>()
            .WithClient<ISimpleRpcServiceClient>();
        rpc.RegisterClients();

        var c = services.BuildServiceProvider();
        _ = c.RpcHub().GetPeer(serverPeerName);
        return c;
    }

    private ChannelPair<RpcMessage> CreateRpcChannelPair()
        => CreateChannelPair<RpcMessage>();

    private ChannelPair<T> CreateChannelPair<T>()
    {
        var options = new UnboundedChannelOptions() {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
        };
        return ChannelPair.CreateTwisted(
            Channel.CreateUnbounded<T>(options),
            Channel.CreateUnbounded<T>(options));
    }
}
