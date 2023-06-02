using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Testing.Output;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Rpc;

public abstract class RpcTestBase : TestBase
{
    protected Symbol ServerPeerName { get; init; } = "server";

    protected RpcTestBase(ITestOutputHelper @out) : base(@out) { }

    protected virtual IServiceProvider CreateServices(
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        configureServices?.Invoke(services);

        var c = services.BuildServiceProvider();
        StartServices(c);
        return c;
    }

    protected virtual void StartServices(IServiceProvider services)
    {
        services.RpcHub().GetPeer("server");
    }

    protected virtual void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddDebug();
            logging.AddProvider(
#pragma warning disable CS0618
                new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor(Out),
                    (_, level) => level >= LogLevel.Debug));
#pragma warning restore CS0618
        });

        var channelPair = CreateRpcChannelPair();
        services.AddSingleton(channelPair);

        var rpc = services.AddRpc();
        rpc.HasPeerFactory(c => {
            var hub = c.RpcHub();
            return name => new RpcPeer(hub, name) {
                LocalServiceFilter = name == ServerPeerName
                    ? _ => true
                    : _ => false
            };
        });
        rpc.HasPeerConnector((peer, _) => {
            var c = peer.Hub.Services;
            var cp = c.GetRequiredService<ChannelPair<RpcMessage>>();
            var channel = peer.Name == ServerPeerName ? cp.Channel1 : cp.Channel2;
            return Task.FromResult(channel);
        });
    }

    protected virtual ChannelPair<RpcMessage> CreateRpcChannelPair()
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
