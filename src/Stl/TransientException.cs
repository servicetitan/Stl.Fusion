namespace Stl;

#pragma warning disable SYSLIB0051

/// <summary>
/// A tagging interface for any exception that might be "cured" by retrying the operation.
/// </summary>
public interface ITransientException;

[Serializable]
public class TransientException : Exception, ITransientException
{
    public TransientException() : this(null) { }
    public TransientException(string? message)
        : base(message ?? "Transient error.") { }
    public TransientException(string? message, Exception innerException)
        : base(message ?? "Transient error.", innerException) { }
    protected TransientException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
