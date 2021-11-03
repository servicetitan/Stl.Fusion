namespace Stl.Caching;

public static class AsyncKeyResolverExt
{
    public static async ValueTask<TValue?> GetOrDefault<TKey, TValue>(
        this IAsyncKeyResolver<TKey, TValue> keyResolver,
        TKey key,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var result = await keyResolver.TryGet(key, cancellationToken).ConfigureAwait(false);
        return result.IsSome(out var value) ? value : default;
    }

    public static async ValueTask<TValue> GetOrDefault<TKey, TValue>(
        this IAsyncKeyResolver<TKey, TValue> keyResolver,
        TKey key,
        TValue @default,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var result = await keyResolver.TryGet(key, cancellationToken).ConfigureAwait(false);
        return result.IsSome(out var value) ? value : @default;
    }
}
