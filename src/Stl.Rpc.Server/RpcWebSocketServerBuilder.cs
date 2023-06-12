using Microsoft.AspNetCore.Routing.Matching;
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

        services.TryAddSingleton(_ => RpcWebSocketServer.Options.Default);
        services.TryAddSingleton(c => new RpcWebSocketServer(c.GetRequiredService<RpcWebSocketServer.Options>(), c));
        if (!services.HasService<EndpointSelector>())
            services.AddRouting();
        configure?.Invoke(this);
    }

    public RpcWebSocketServerBuilder Configure(Func<IServiceProvider, RpcWebSocketServer.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }
}
