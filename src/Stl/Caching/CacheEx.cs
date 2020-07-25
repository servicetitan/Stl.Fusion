using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public static class CacheEx
    {
        public static async IAsyncEnumerable<TValue> GetManyAsync<TKey, TValue>(
            this IAsyncKeyResolver<TKey, TValue> cache,
            IAsyncEnumerable<TKey> keys,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TKey : notnull
        {
            await foreach (var key in keys.ConfigureAwait(false))
                yield return await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        }

        public static async IAsyncEnumerable<Option<TValue>> TryGetManyAsync<TKey, TValue>(
            this IAsyncKeyResolver<TKey, TValue> cache,
            IAsyncEnumerable<TKey> keys,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TKey : notnull
        {
            await foreach (var key in keys.ConfigureAwait(false))
                yield return await cache.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }
}
