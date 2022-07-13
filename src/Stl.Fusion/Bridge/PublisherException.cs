namespace Stl.Fusion.Bridge;

[Serializable]
public class PublisherException : Exception, ITransientException
{
    public PublisherException() { }
    public PublisherException(string? message) : base(message) { }
    public PublisherException(string? message, Exception? innerException) : base(message, innerException) { }
    protected PublisherException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
