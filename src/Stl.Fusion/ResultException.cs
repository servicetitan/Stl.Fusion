namespace Stl.Fusion;

public class ResultException : Exception, IWrappedException
{
    public ResultException() { }
    public ResultException(string? message) : base(message) { }
    public ResultException(string? message, Exception? innerException) : base(message, innerException) { }
    protected ResultException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public Exception Unwrap()
        => InnerException ?? this;
}
