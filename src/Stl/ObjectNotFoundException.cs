namespace Stl;

public class ObjectNotFoundException : Exception
{
    public ObjectNotFoundException() : base("Object not found.") { }
    public ObjectNotFoundException(string? message) : base(message) { }
    public ObjectNotFoundException(string? message, Exception? innerException) : base(message, innerException) { }
    protected ObjectNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
