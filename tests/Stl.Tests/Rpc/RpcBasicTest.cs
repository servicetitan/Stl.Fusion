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
        var service = services.GetRequiredService<ISimpleRpcService>();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        // var sum = await client.Sum(1, 2);
        // sum.Should().Be(3);
    }

    private IServiceProvider CreateServerServices()
    {
        var services = new ServiceCollection();
        var channelPair = CreateRpcChannelPair();
        services.AddSingleton(channelPair);
        services.AddSingleton<ISimpleRpcService, SimpleRpcService>();

        var rpc = services.AddRpc();
        rpc.HasConnector((peer, _) => {
            var c = peer.Hub.Services;
            var cp = c.GetRequiredService<ChannelPair<RpcMessage>>();
            return peer.Name.Value switch {
                "" => Task.FromResult(cp.Channel1),
                "server" => Task.FromResult(cp.Channel2),
                _ => throw new KeyNotFoundException(),
            };
        });
        rpc.HasService<ISimpleRpcService>()
            .Serving<SimpleRpcService>()
            .ConsumedAs<ISimpleRpcServiceClient>();
        rpc.RegisterClients();
        return services.BuildServiceProvider();
    }

    private ChannelPair<RpcMessage> CreateRpcChannelPair()
        => CreateChannelPair<RpcMessage>();

    private ChannelPair<T> CreateChannelPair<T>()
    {
        var options = new BoundedChannelOptions(1) {
            AllowSynchronousContinuations = true,
        };
        return ChannelPair.CreateTwisted(
            Channel.CreateBounded<T>(options),
            Channel.CreateBounded<T>(options));
    }
}
