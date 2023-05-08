namespace Stl.Rpc.Internal;

[Serializable]
public class ImpossibleToReconnectException : Exception
{
    public ImpossibleToReconnectException()
        : this(message: null, innerException: null) { }
    public ImpossibleToReconnectException(string? message)
        : this(message, innerException: null) { }
    public ImpossibleToReconnectException(string? message, Exception? innerException)
        : base(message ?? "Impossible to reconnect.", innerException) { }
    protected ImpossibleToReconnectException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
