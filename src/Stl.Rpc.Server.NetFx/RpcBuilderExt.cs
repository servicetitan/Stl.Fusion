namespace Stl.Rpc.Server;

public static class RpcBuilderExt
{
    public static RpcServerBuilder AddServer(this RpcBuilder rpc)
        => new(rpc, null);

    public static RpcBuilder AddWebServer(this RpcBuilder rpc, Action<RpcServerBuilder> configure)
        => new RpcServerBuilder(rpc, configure).Rpc;
}
