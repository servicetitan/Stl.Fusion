using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Testing.Output;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Rpc;

public abstract class RpcLocalTestBase : TestBase
{
    protected Symbol ServerPeerId { get; init; } = RpcServerPeer.FormatId("client");

    protected RpcLocalTestBase(ITestOutputHelper @out) : base(@out) { }

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
        var channels = services.GetRequiredService<ChannelPair<RpcMessage>>();
        var serverPeer = (RpcServerPeer)services.RpcHub().GetPeer(ServerPeerId);
        serverPeer.SetConnectionState(channels.Channel1);
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

        var rpc = services.AddRpc();
        var channelPair = CreateRpcChannelPair();
        services.AddSingleton(channelPair);
        rpc.UseClientChannelProvider((peer, _) => {
            var c = peer.Hub.Services;
            var channels = c.GetRequiredService<ChannelPair<RpcMessage>>();
            return Task.FromResult(channels.Channel2);
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
