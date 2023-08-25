namespace Stl.Rpc;

public interface IRpcStreamResolver
{
    ValueTask<object?> TryGet(RpcStreamId streamId);
}
