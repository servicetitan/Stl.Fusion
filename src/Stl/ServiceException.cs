namespace Stl;

public class ServiceException : Exception, IWrappedException
{
    public ServiceException() { }
    public ServiceException(string? message) : base(message) { }
    public ServiceException(string? message, Exception? innerException) : base(message, innerException) { }
    protected ServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public Exception Unwrap()
        => InnerException ?? this;
}
