#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcCall(RpcMethodDef methodDef)
{
    protected object Lock => this;

    public readonly RpcMethodDef MethodDef = methodDef;
    public RpcHub Hub => MethodDef.Hub;
    public RpcServiceDef ServiceDef => MethodDef.Service;
    public long Id;
    public readonly bool NoWait = methodDef.NoWait; // Copying it here just b/c it's frequently accessed
}
