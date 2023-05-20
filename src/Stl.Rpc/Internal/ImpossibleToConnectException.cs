namespace Stl.Rpc.Internal;

[Serializable]
public class ImpossibleToConnectException : Exception
{
    public ImpossibleToConnectException()
        : this(message: null, innerException: null) { }
    public ImpossibleToConnectException(string? message)
        : this(message, innerException: null) { }
    public ImpossibleToConnectException(string? message, Exception? innerException)
        : base(message ?? "Impossible to (re)connect.", innerException) { }
    protected ImpossibleToConnectException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
