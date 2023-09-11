using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public enum RpcObjectKind
{
    Local = 0,
    Remote,
}

public interface IRpcObject : IHasId<long>
{
    RpcObjectKind Kind { get; }
    ValueTask OnReconnected(CancellationToken cancellationToken);
    void OnMissing();
}

public interface IRpcSharedObject : IRpcObject
{
    CpuTimestamp LastKeepAliveAt { get; }
    void OnKeepAlive();
}

public static class RpcObjectExt
{
    public static void RequireKind(this IRpcObject rpcObject, RpcObjectKind expectedKind)
    {
        if (rpcObject.Kind != expectedKind)
            throw Errors.InvalidRpcObjectKind(expectedKind);
    }
}
