namespace Stl.Pooling;

public static class SharedResourcePoolExt
{
    public static async Task<SharedResourceHandle<TKey, TResource>> Acquire<TKey, TResource>(
        this ISharedResourcePool<TKey, TResource> pool,
        TKey key, CancellationToken cancellationToken = default)
    {
        var handle = await pool.TryAcquire(key, cancellationToken).ConfigureAwait(false);
        return handle.IsValid ? handle : throw new KeyNotFoundException();
    }
}
