namespace Stl.Caching;

public abstract class AsyncCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>, IAsyncCache<TKey, TValue>
    where TKey : notnull
{
    public ValueTask Set(TKey key, TValue value, CancellationToken cancellationToken = default)
        => Set(key, Option.Some(value), cancellationToken);

    public ValueTask Remove(TKey key, CancellationToken cancellationToken = default)
        => Set(key, Option.None<TValue>(), cancellationToken);

    protected abstract ValueTask Set(TKey key, Option<TValue> value, CancellationToken cancellationToken = default);
}
