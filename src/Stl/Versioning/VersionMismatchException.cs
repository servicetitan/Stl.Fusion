namespace Stl.Versioning;

[Serializable]
public class VersionMismatchException : Exception
{
    public VersionMismatchException()
        : this(message: null, innerException: null) { }
    public VersionMismatchException(string? message)
        : this(message, innerException: null) { }
    public VersionMismatchException(string? message, Exception? innerException)
        : base(message ?? "Version mismatch.", innerException) { }
    protected VersionMismatchException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
