namespace Stl.Async;

[Serializable]
public class AsyncEventSequenceCompletedException : Exception
{
    public AsyncEventSequenceCompletedException()
        : this(message: null, innerException: null) { }
    public AsyncEventSequenceCompletedException(string? message)
        : this(message, innerException: null) { }
    public AsyncEventSequenceCompletedException(string? message, Exception? innerException)
        : base(message ?? "Async event sequence is completed.", innerException) { }
    protected AsyncEventSequenceCompletedException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
