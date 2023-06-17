#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcCall
{
    public RpcHub Hub => MethodDef.Hub;
    public RpcServiceDef ServiceDef => MethodDef.Service;
    public RpcMethodDef MethodDef { get; }
    public long Id { get; protected set; }

    public bool NoWait {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id == 0;
    }

    protected RpcCall(RpcMethodDef methodDef)
        => MethodDef = methodDef;
}
