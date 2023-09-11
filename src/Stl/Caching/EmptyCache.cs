namespace Stl.Caching;

public class EmptyCache<TKey, TValue> : AsyncCacheBase<TKey, TValue>
    where TKey : notnull
{
    public override ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
        => ValueTaskExt.FromResult(Option.None<TValue>());

    protected override ValueTask Set(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
        => default;
}
