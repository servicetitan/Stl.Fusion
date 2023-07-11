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

    public static Exception ConnectionUnrecoverable(Exception? innerException = null)
        => new ConnectionUnrecoverableException(innerException);

    public static Exception NoCurrentRpcInboundContext()
        => new InvalidOperationException($"{nameof(RpcInboundContext)}.{nameof(RpcInboundContext.Current)} is unavailable.");
    public static Exception RpcOutboundContextChanged()
        => new InvalidOperationException(
            $"The scope returned from {nameof(RpcOutboundContext)}.{nameof(RpcOutboundContext.Use)} " +
            $"detected context change on its disposal. " +
            $"Most likely the scope was disposed in async continuation / another thread, which should never happen - " +
            $"this scope should be used only in synchronous part of your code that happens " +
            $"right before the async method triggering the outgoing RPC call is invoked.");

    public static Exception InvalidMessageSize()
        => new SerializationException("Invalid item size. The remainder of the message will be dropped.");
    public static Exception CannotDeserializeUnexpectedArgumentType(Type expectedType, Type actualType)
        => new SerializationException($"Cannot deserialize unexpected argument type: " +
            $"expected '{expectedType.GetName()}' (exact match), got '{actualType.GetName()}'.");
    public static Exception CannotDeserializeUnexpectedPolymorphicArgumentType(Type expectedType, Type actualType)
        => new SerializationException($"Cannot deserialize polymorphic argument type: " +
            $"expected '{expectedType.GetName()}' or its descendant, got '{actualType.GetName()}'.");

    public static Exception CallTimeout(RpcPeer peer)
        => CallTimeout(peer.Ref.IsServer ? "client" : "server");
    public static Exception CallTimeout(string partyName = "server")
        => new TimeoutException($"The {partyName} didn't respond in time.");

    public static Exception Disconnected(RpcPeer peer)
        => Disconnected(peer.Ref.IsServer ? "client" : "server");
    public static Exception Disconnected(string partyName = "server")
        => new DisconnectedException($"The remote {partyName} is disconnected.");

    public static Exception ClientRpcPeerRefExpected(string argumentName)
        => new ArgumentOutOfRangeException(argumentName, "Client RpcPeerRef (with IsServer == false) is expected here.");
    public static Exception ServerRpcPeerRefExpected(string argumentName)
        => new ArgumentOutOfRangeException(argumentName, "Server RpcPeerRef (with IsServer == true) is expected here.");
}
