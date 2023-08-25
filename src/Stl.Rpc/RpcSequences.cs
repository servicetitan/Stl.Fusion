namespace Stl.Rpc;

public class RpcSequences
{
    public async Task<Channel<T>?> TryGet<T>(Symbol channelId, CancellationToken cancellationToken = default)
    {
        var result = await TryGet(channelId, typeof(T), cancellationToken).ConfigureAwait(false);
        return (Channel<T>?)result;
    }

    public virtual Task<object?> TryGet(Symbol channelId, TypeRef itemType, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
