namespace Stl.Rpc;

[Serializable]
public class DisconnectedException : Exception, ITransientException
{
    public DisconnectedException()
        : this(message: null, innerException: null) { }
    public DisconnectedException(string? message)
        : this(message, innerException: null) { }
    public DisconnectedException(string? message, Exception? innerException)
        : base(message ?? "Connection to remote server is offline.", innerException) { }
    protected DisconnectedException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
