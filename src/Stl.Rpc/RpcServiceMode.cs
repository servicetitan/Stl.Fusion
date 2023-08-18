namespace Stl.Rpc;

public enum RpcServiceMode
{
    Default = 0,
    None,
    Server,
    Router,
    RoutingServer,
    ServingRouter,
}

public static class RpcServiceShareModeExt
{
    public static RpcServiceMode Or(this RpcServiceMode mode, RpcServiceMode defaultMode)
        => mode == RpcServiceMode.Default ? defaultMode.OrNone() : mode;

    public static RpcServiceMode OrNone(this RpcServiceMode mode)
        => mode == RpcServiceMode.Default ? RpcServiceMode.None : mode;
}
