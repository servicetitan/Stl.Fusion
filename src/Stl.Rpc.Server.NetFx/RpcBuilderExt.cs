namespace Stl.Rpc.Server;

public static class RpcBuilderExt
{
    public static RpcWebSocketServerBuilder UseWebSocketServer(this RpcBuilder rpc)
        => new(rpc, null);

    public static RpcBuilder UseWebSocketServer(this RpcBuilder rpc, Action<RpcWebSocketServerBuilder> configure)
        => new RpcWebSocketServerBuilder(rpc, configure).Rpc;
}
