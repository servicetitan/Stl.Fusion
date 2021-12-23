namespace Stl.Serialization.Internal;

public static class Errors
{
    public static Exception NoSerializer()
        => new NotSupportedException("No serializer provided.");

    public static Exception UnsupportedSerializedType(Type type)
        => new SerializationException($"Unsupported type: '{type}'.");

    public static Exception SerializedTypeMismatch(Type supportedType, Type requestedType)
        => new NotSupportedException(
            $"The serializer implements '{supportedType}' serialization, but '{requestedType}' was requested to (de)serialize.");

    public static Exception RemoteException(ExceptionInfo exceptionInfo)
        => new RemoteException(exceptionInfo, $"Remote exception: {exceptionInfo.ToString()}");
}
