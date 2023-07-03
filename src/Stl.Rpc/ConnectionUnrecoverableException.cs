namespace Stl.Rpc;

[Serializable]
public class ConnectionUnrecoverableException : Exception
{
    public ConnectionUnrecoverableException()
        : this(message: null, innerException: null) { }
    public ConnectionUnrecoverableException(string? message)
        : this(message, innerException: null) { }
    public ConnectionUnrecoverableException(Exception? innerException)
        : base(innerException == null
            ? "Impossible to (re)connect."
            : $"Impossible to (re)connect: {innerException.Message}", innerException) { }
    public ConnectionUnrecoverableException(string? message, Exception? innerException)
        : base(message ?? "Impossible to (re)connect.", innerException) { }
    protected ConnectionUnrecoverableException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
