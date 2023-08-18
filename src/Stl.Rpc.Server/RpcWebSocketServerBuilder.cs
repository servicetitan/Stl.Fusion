using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Rpc.Server;

public readonly struct RpcWebSocketServerBuilder
{
    public RpcBuilder Rpc { get; }
    public IServiceCollection Services => Rpc.Services;

    internal RpcWebSocketServerBuilder(
        RpcBuilder rpc,
        Action<RpcWebSocketServerBuilder>? configure)
    {
        Rpc = rpc;
        var services = Services;
        if (services.HasService<RpcWebSocketServer>()) {
            configure?.Invoke(this);
            return;
        }

        services.TryAddSingleton(_ => RpcWebSocketServerDefaultDelegates.PeerRefFactory);
        services.TryAddSingleton(_ => RpcWebSocketServer.Options.Default);
        services.TryAddSingleton(c => new RpcWebSocketServer(c.GetRequiredService<RpcWebSocketServer.Options>(), c));
        configure?.Invoke(this);
    }

    public RpcWebSocketServerBuilder Configure(Func<IServiceProvider, RpcWebSocketServer.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }
}
