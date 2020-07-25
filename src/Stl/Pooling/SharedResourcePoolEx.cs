using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Pooling
{
    public static class SharedResourcePoolEx
    {
        public static async Task<SharedResourceHandle<TKey, TResource>> AcquireAsync<TKey, TResource>(
            this ISharedResourcePool<TKey, TResource> pool,
            TKey key, CancellationToken cancellationToken = default)
        {
            var handle = await pool.TryAcquireAsync(key, cancellationToken).ConfigureAwait(false);
            return handle.IsValid ? handle : throw new KeyNotFoundException();
        }
    }
}
