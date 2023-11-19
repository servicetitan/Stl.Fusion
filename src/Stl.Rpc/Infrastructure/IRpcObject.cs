using System.Diagnostics.CodeAnalysis;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public enum RpcObjectKind
{
    Local = 0,
    Remote,
}

public interface IRpcObject : IHasId<RpcObjectId>
{
    RpcObjectKind Kind { get; }
    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    Task Reconnect(CancellationToken cancellationToken);
    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    void Disconnect();
}

public interface IRpcSharedObject : IRpcObject
{
    CpuTimestamp LastKeepAliveAt { get; }
    void KeepAlive();
}

public static class RpcObjectExt
{
    public static void RequireKind(this IRpcObject rpcObject, RpcObjectKind expectedKind)
    {
        if (rpcObject.Kind != expectedKind)
            throw Errors.InvalidRpcObjectKind(expectedKind);
    }
}
