using Stl.Interception;
using Stl.Interception.Interceptors;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public class RpcMethodDef : MethodDef
{
    private static readonly MethodInfo CreateBoundRequestFactoryMethod =
        typeof(RpcMethodDef).GetMethod(nameof(CreateBoundRequestFactory), BindingFlags.Static | BindingFlags.NonPublic)!;

    public RpcServiceDef Service { get; }
    public Symbol Name { get; }

    public Type ArgumentListType { get; }
    public Type[] RemoteParameterTypes { get; }
    public Type RemoteArgumentListType { get; }
    public bool MustCheckArguments { get; }
    public Func<ArgumentList, RpcBoundRequest> BoundRequestFactory { get; }

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
        BoundRequestFactory = (Func<ArgumentList, RpcBoundRequest>)CreateBoundRequestFactoryMethod
            .MakeGenericMethod(UnwrappedReturnType)
            .Invoke(null, new object?[] { this })!;

        Service = service;
        Name = methodNameBuilder.Invoke(this);

        if (!IsAsyncMethod)
            IsValid = false;
    }

    public virtual void CheckArguments(RpcPeer peer, RpcMessage message, Type[] argumentTypes)
    { }

    // Private methods

    private static Func<ArgumentList, RpcBoundRequest> CreateBoundRequestFactory<T>(RpcMethodDef methodDef)
        => arguments => new RpcBoundRequest<T>(methodDef, arguments);
}
