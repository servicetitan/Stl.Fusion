namespace Stl.Rpc;

[Serializable]
public class RpcException : Exception
{
    public RpcException() : this(null) { }
    public RpcException(string? message)
        : base(message ?? "RPC error.") { }
    public RpcException(string? message, Exception innerException)
        : base(message ?? "RPC error.", innerException) { }
    protected RpcException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
