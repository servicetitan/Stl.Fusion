namespace Stl.Rpc.Server;

public static class RpcBuilderExt
{
    public static RpcServerBuilder AddServer(this RpcBuilder rpc)
        => new(rpc, null);

    public static RpcBuilder AddWebServer(this RpcBuilder fusion, Action<RpcServerBuilder> configure)
        => new RpcServerBuilder(fusion, configure).Rpc;
}
