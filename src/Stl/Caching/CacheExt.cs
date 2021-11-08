namespace Stl.Caching;

public static class CacheExt
{
    public static async IAsyncEnumerable<TValue> GetManyAsync<TKey, TValue>(
        this IAsyncKeyResolver<TKey, TValue> cache,
        IAsyncEnumerable<TKey> keys,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        await foreach (var key in keys.ConfigureAwait(false)) {
            var valueOpt = await cache.TryGet(key, cancellationToken).ConfigureAwait(false);
            yield return valueOpt.IsSome(out var value) ? value : throw new KeyNotFoundException();
        }
    }

    public static async IAsyncEnumerable<Option<TValue>> TryGetManyAsync<TKey, TValue>(
        this IAsyncKeyResolver<TKey, TValue> cache,
        IAsyncEnumerable<TKey> keys,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        await foreach (var key in keys.ConfigureAwait(false))
            yield return await cache.TryGet(key, cancellationToken).ConfigureAwait(false);
    }
}
