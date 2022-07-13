namespace Stl.Fusion.Bridge;

[Serializable]
public class ReplicaException : Exception, ITransientException
{
    public ReplicaException() { }
    public ReplicaException(string? message) : base(message) { }
    public ReplicaException(string? message, Exception? innerException) : base(message, innerException) { }
    protected ReplicaException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
