namespace Stl.Caching;

public static class CacheExt
{
    public static async IAsyncEnumerable<TValue> GetManyAsync<TKey, TValue>(
        this IAsyncKeyResolver<TKey, TValue> cache,
        IAsyncEnumerable<TKey> keys,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        await foreach (var key in keys.ConfigureAwait(false))
            yield return await cache.Get(key, cancellationToken).ConfigureAwait(false);
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
