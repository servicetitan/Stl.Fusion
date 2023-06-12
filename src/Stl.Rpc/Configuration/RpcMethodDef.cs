using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Rpc;

public sealed class RpcMethodDef : MethodDef
{
    public RpcHub Hub { get; }
    public RpcServiceDef Service { get; }
    public Symbol Name { get; }

    public Type ArgumentListType { get; }
    public Type[] RemoteParameterTypes { get; }
    public Type RemoteArgumentListType { get; }
    public bool HasObjectTypedArguments { get; }
    public bool NoWait { get; }

    public RpcMethodDef(RpcServiceDef service, MethodInfo method)
        : base(service.Type, method)
    {
        Hub = service.Hub;
        ArgumentListType = Parameters.Length == 0
            ? ArgumentList.Types[0]
            : ArgumentList.Types[Parameters.Length].MakeGenericType(ParameterTypes);
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
        HasObjectTypedArguments = RemoteParameterTypes.Any(type => typeof(object) == type);
        NoWait = UnwrappedReturnType == typeof(RpcNoWait);

        Service = service;
        Name = Hub.MethodNameBuilder.Invoke(this);

        if (!IsAsyncMethod)
            IsValid = false;
    }
}
