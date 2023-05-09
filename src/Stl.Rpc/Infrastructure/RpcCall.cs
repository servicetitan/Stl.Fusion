using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public interface IRpcCall
{
    RpcMethodDef MethodDef { get; }
    RpcServiceDef ServiceDef { get; }
    RpcHub Hub { get; }

    Task Start();
}

public abstract class RpcCall<T> : IRpcCall
{
    public RpcMethodDef MethodDef { get; }
    public RpcServiceDef ServiceDef => MethodDef.Service;
    public RpcHub Hub => MethodDef.Hub;

    protected RpcCall(RpcMethodDef methodDef)
        => MethodDef = methodDef;

    public abstract Task Start();
}
