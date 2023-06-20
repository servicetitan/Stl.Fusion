namespace Stl.Rpc.Testing;

public static class RpcBuilderExt
{
    public static RpcBuilder AddTestClient(this RpcBuilder rpc,
        Func<IServiceProvider, RpcTestClient.Options>? optionsFactory = null)
    {
        var services = rpc.Services;
        services.AddSingleton(optionsFactory, _ => RpcTestClient.Options.Default);
        if (services.HasService<RpcTestClient>())
            return rpc;

        services.AddSingleton(c => new RpcTestClient(
            c.GetRequiredService<RpcTestClient.Options>(), c));
        services.AddAlias<RpcClient, RpcTestClient>();
        services.AddSingleton<RpcPeerFactory>(_ => (hub, peerRef)
            => peerRef.IsServer
                ? new RpcServerPeer(hub, peerRef) {
                    CloseTimeout = TimeSpan.FromSeconds(10),
                }
                : new RpcClientPeer(hub, peerRef) {
                    ReconnectDelays = new(TimeSpan.Zero, TimeSpan.Zero, 0),
                });
        return rpc;
    }
}
