using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public static class Errors
{
    public static Exception RpcOptionsMustBeRegisteredAsInstance()
        => new InvalidOperationException("RpcOptions should be registered as instance.");
    public static Exception RpcOptionsIsNotRegistered()
        => new InvalidOperationException("RpcOptions instance is not registered.");

    public static Exception UnknownCallType(byte callTypeId)
        => new KeyNotFoundException($"Unknown CallTypeId: {callTypeId}.");

    public static Exception ServiceAlreadyExists(Type type)
        => new InvalidOperationException($"Service of type '{type}' is already added.");
    public static Exception ServiceTypeCannotBeChanged(Type originalType, Type type)
        => new InvalidOperationException(
            $"RpcServiceConfiguration.Type is changed from the original '{originalType}' to '{type}'.");

    public static Exception ServiceTypeConflict(Type serviceType)
        => new InvalidOperationException($"Service '{serviceType.GetName()}' is already registered.");
    public static Exception ServiceNameConflict(Type serviceType1, Type serviceType2, Symbol serviceName)
        => new InvalidOperationException($"Services '{serviceType1.GetName()}' and '{serviceType2.GetName()}' have the same name '{serviceName}'.");
    public static Exception MethodNameConflict(RpcMethodDef methodDef)
        => new InvalidOperationException($"Service '{methodDef.Service.Type.GetName()}' has 2 or more methods named '{methodDef.Name}'.");

    public static Exception NoService(Type serviceType)
        => new KeyNotFoundException($"Can't resolve service by type: '{serviceType.GetName()}'.");
    public static Exception NoService(string serviceName)
        => new KeyNotFoundException($"Can't resolve service by name: '{serviceName}'.");

    public static Exception NoMethod(Type serviceType, MethodInfo method)
        => new KeyNotFoundException($"Can't resolve method '{method.Name}' (by MethodInfo) of '{serviceType.GetName()}'.");
    public static Exception NoMethod(Type serviceType, string methodName)
        => new KeyNotFoundException($"Can't resolve method '{methodName}' (by name) of '{serviceType.GetName()}'.");

    public static Exception EndpointNotFound(string serviceName, string methodName)
        => new RpcException($"Endpoint not found: '{serviceName}.{methodName}'.");

    public static Exception AlreadyConnected()
        => new InvalidOperationException($"This {nameof(RpcPeer)} is already connected.");
    public static Exception ConnectionTimeout()
        => new TimeoutException($"Connection time-out.");
    public static Exception ConnectionTimeout(TimeSpan timeout)
        => new TimeoutException($"Connection time-out ({timeout.ToShortString()}).");
    public static Exception ConnectionRetryLimitExceeded()
        => new ConnectionUnrecoverableException("Can't reconnect: retry limit exceeded.");
    public static Exception ConnectionUnrecoverable()
        => new ConnectionUnrecoverableException();

    public static Exception NoCurrentRpcInboundContext()
        => new InvalidOperationException($"{nameof(RpcInboundContext)}.{nameof(RpcInboundContext.Current)} is unavailable.");
    public static Exception RpcOutboundContextChanged()
        => new InvalidOperationException(
            $"The scope returned from {nameof(RpcOutboundContext)}.{nameof(RpcOutboundContext.Use)} " +
            $"detected context change on its disposal. " +
            $"Most likely the scope was disposed in async continuation / another thread, which should never happen - " +
            $"this scope should be used only in synchronous part of your code that happens " +
            $"right before the async method triggering the outgoing RPC call is invoked.");

    public static Exception IncompatibleArgumentType(RpcMethodDef methodDef, int argumentIndex, Type argumentType)
        => new InvalidOperationException(
            $"Argument #{argumentIndex} for '{methodDef.FullName}' has incompatible type: '{argumentType.GetName()}.'");
    public static Exception NonDeserializableArguments(RpcMethodDef methodDef)
        => new InvalidOperationException($"Couldn't deserialize arguments for '{methodDef.FullName}'.");
    public static Exception IncompatibleResultType(RpcMethodDef methodDef, Type actualResultType)
        => new InvalidOperationException($"Couldn't deserialize result for '{methodDef.FullName}' call: " +
            $"expected '{methodDef.UnwrappedReturnType.GetName()}', " +
            $"but got '{actualResultType.GetName()}'.");

    public static Exception InvalidMessageSize()
        => new SerializationException("Invalid item size. The remainder of the message will be dropped.");

    public static Exception TestConnectionIsTerminated()
        => new InvalidOperationException("Test connection is terminated.");
}
