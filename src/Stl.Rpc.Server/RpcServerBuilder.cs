using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Rpc.Server;

public readonly struct RpcServerBuilder
{
    public RpcBuilder Rpc { get; }
    public IServiceCollection Services => Rpc.Services;

    internal RpcServerBuilder(
        RpcBuilder rpc,
        Action<RpcServerBuilder>? configure)
    {
        Rpc = rpc;
        var services = Services;
        if (services.HasService<RpcServer>()) {
            configure?.Invoke(this);
            return;
        }

        services.TryAddSingleton<RpcServer.Options>();
        services.TryAddSingleton<RpcServer>();
        configure?.Invoke(this);
    }

    public RpcServerBuilder Configure(Func<IServiceProvider, RpcServer.Options> serverOptionsFactory)
    {
        Services.AddSingleton(serverOptionsFactory);
        return this;
    }
}
