#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcCall
{
    protected object Lock => this;

    public readonly RpcMethodDef MethodDef;
    public RpcHub Hub => MethodDef.Hub;
    public RpcServiceDef ServiceDef => MethodDef.Service;
    public long Id;
    public readonly bool NoWait; // Copying it here just b/c it's frequently accessed

    protected RpcCall(RpcMethodDef methodDef)
    {
        MethodDef = methodDef;
        NoWait = methodDef.NoWait;
    }
}
