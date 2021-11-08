namespace Stl.Caching;

public class MemoizingCache<TKey, TValue> : AsyncCacheBase<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();

    public override ValueTask<TValue?> Get(TKey key, CancellationToken cancellationToken = default)
        => ValueTaskExt.FromResult(_dictionary[key])!;

    public override ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
        => ValueTaskExt.FromResult(
            _dictionary.TryGetValue(key, out var value)
                ? Option.Some(value)
                : default);

    protected override ValueTask Set(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
    {
        if (value.IsSome(out var v))
            _dictionary[key] = v;
        else
            _dictionary.TryRemove(key, out _);
        return ValueTaskExt.CompletedTask;
    }
}
