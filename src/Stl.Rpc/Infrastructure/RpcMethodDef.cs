using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcMethodDef : MethodDef
{
    public RpcServiceDef Service { get; }
    public Symbol Name { get; }

    public Type ArgumentListType { get; }
    public Type[] RemoteParameterTypes { get; }
    public Type RemoteArgumentListType { get; }
    public bool MustCheckArguments { get; }

    public RpcMethodDef(RpcServiceDef service, MethodInfo method, Func<RpcMethodDef, Symbol> methodNameBuilder)
        : base(service.Type, method)
    {
        ArgumentListType = ArgumentList.Types[Parameters.Length].MakeGenericType(ParameterTypes);
        if (CancellationTokenIndex >= 0) {
            var remoteParameterTypes = new Type[ParameterTypes.Length - 1];
            for (var i = 0; i < ParameterTypes.Length; i++) {
                if (i < CancellationTokenIndex)
                    remoteParameterTypes[i] = ParameterTypes[i];
                else if (i > CancellationTokenIndex)
                    remoteParameterTypes[i - 1] = ParameterTypes[i];
            }
            RemoteParameterTypes = remoteParameterTypes;
            RemoteArgumentListType = ArgumentList
                .Types[remoteParameterTypes.Length]
                .MakeGenericType(remoteParameterTypes);
        }
        else {
            RemoteParameterTypes = ParameterTypes;
            RemoteArgumentListType = ArgumentListType;
        }

        Service = service;
        Name = methodNameBuilder.Invoke(this);

        if (!IsAsyncMethod)
            IsValid = false;
    }

    public virtual void CheckArguments(RpcRequest request, RpcChannel channel, Type[] argumentTypes)
    { }
}
