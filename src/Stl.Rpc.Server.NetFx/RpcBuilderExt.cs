namespace Stl.Rpc.Server;

public static class RpcBuilderExt
{
    public static RpcWebSocketServerBuilder AddWebSocketServer(this RpcBuilder rpc)
        => new(rpc, null);

    public static RpcBuilder AddWebSocketServer(this RpcBuilder rpc, Action<RpcWebSocketServerBuilder> configure)
        => new RpcWebSocketServerBuilder(rpc, configure).Rpc;
}
