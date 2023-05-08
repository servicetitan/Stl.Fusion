using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public static class Errors
{
    public static Exception RpcOptionsMustBeRegisteredAsInstance()
        => new InvalidOperationException("RpcOptions should be registered as instance.");
    public static Exception RpcOptionsIsNotRegistered()
        => new InvalidOperationException("RpcOptions instance is not registered.");

    public static Exception ServiceTypeConflict(Type serviceType)
        => new InvalidOperationException($"Service '{serviceType.GetName()}' is already registered.");
    public static Exception ServiceNameConflict(Type serviceType1, Type serviceType2, Symbol serviceName)
        => new InvalidOperationException($"Services '{serviceType1.GetName()}' and '{serviceType2.GetName()}' have the same name '{serviceName}'.");
    public static Exception MethodNameConflict(RpcMethodDef methodDef)
        => new InvalidOperationException($"Service '{methodDef.Service.Type.GetName()}' has 2 or more methods named '{methodDef.Name}'.");

    public static Exception NoService(Type serviceType)
        => new InvalidOperationException($"Can't resolve service by type: '{serviceType.GetName()}'.");
    public static Exception NoService(string serviceName)
        => new InvalidOperationException($"Can't resolve service by name: '{serviceName}'.");
    public static Exception ServiceIsNotWhiteListed(RpcServiceDef serviceDef)
        => new InvalidOperationException($"Service '{serviceDef.Type.GetName()}' isn't white-listed.");

    public static Exception NoMethod(Type serviceType, MethodInfo method)
        => new InvalidOperationException($"Can't resolve method '{method.Name}' (by MethodInfo) of '{serviceType.GetName()}'.");
    public static Exception NoMethod(Type serviceType, string methodName)
        => new InvalidOperationException($"Can't resolve method '{methodName}' (by name) of '{serviceType.GetName()}'.");

    public static Exception AlreadyConnected()
        => new InvalidOperationException($"This {nameof(RpcPeer)} is already connected.");
    public static Exception ConnectionIsClosed()
        => new InvalidOperationException("Connection is gracefully closed by peer.");
    public static Exception ImpossibleToReconnect()
        => new ImpossibleToReconnectException();

    public static Exception NoCurrentRpcRequestContext()
        => new InvalidOperationException($"{nameof(RpcInboundContext)}.{nameof(RpcInboundContext.Current)} is unavailable.");

    public static Exception IncompatibleArgumentType(RpcMethodDef methodDef, int argumentIndex, Type argumentType)
        => new InvalidOperationException(
            $"Argument #{argumentIndex} for '{methodDef.FullName}' has incompatible type: '{argumentType.GetName()}.'");
    public static Exception NonDeserializableArguments(RpcMethodDef methodDef)
        => new InvalidOperationException($"Couldn't deserialize arguments for '{methodDef.FullName}'.");
}
