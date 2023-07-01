namespace Stl.Net;

[Serializable]
public class RetryLimitExceededException : Exception
{
    public RetryLimitExceededException() : this(message: null, innerException: null) { }
    public RetryLimitExceededException(string? message) : this(message, innerException: null) { }
    public RetryLimitExceededException(Exception? innerException)
        : base("Retry limit exceeded.", innerException) { }
    public RetryLimitExceededException(string? message, Exception? innerException)
        : base(message ?? "Retry limit exceeded.", innerException) { }
    protected RetryLimitExceededException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
