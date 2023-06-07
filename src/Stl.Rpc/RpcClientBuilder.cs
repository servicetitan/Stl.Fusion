using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Rpc;

public readonly struct RpcClientBuilder
{
    public RpcBuilder Rpc { get; }
    public IServiceCollection Services => Rpc.Services;

    internal RpcClientBuilder(
        RpcBuilder rpc,
        Action<RpcClientBuilder>? configure)
    {
        Rpc = rpc;
        var services = Services;
        if (services.HasService<RpcClient>()) {
            configure?.Invoke(this);
            return;
        }

        services.TryAddSingleton(_ => RpcClient.Options.Default);
        services.TryAddSingleton(c => new RpcClient(c.GetRequiredService<RpcClient.Options>(), c));
        rpc.HasClientChannelProvider(c => {
            var client = c.GetRequiredService<RpcClient>();
            return client.GetChannel;
        });
        configure?.Invoke(this);
    }

    public RpcClientBuilder Configure(Func<IServiceProvider, RpcClient.Options> serverOptionsFactory)
    {
        Services.AddSingleton(serverOptionsFactory);
        return this;
    }
}
