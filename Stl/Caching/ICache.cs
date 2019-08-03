using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Optional;

namespace Stl.Caching
{
    public interface ICache<in TKey, TValue>
        where TKey : notnull
    {
        ValueTask<Option<TValue>> GetAsync([NotNull] TKey key, CancellationToken cancellationToken = default);
        ValueTask SetAsync([NotNull] TKey key, Option<TValue> value, CancellationToken cancellationToken = default);
    }

    public static class CacheEx 
    {
        public static ValueTask InvalidateAsync<TKey, TValue>(this ICache<TKey, TValue> cache, 
            TKey key, CancellationToken cancellationToken = default)
            where TKey : notnull
            => cache.SetAsync(key, Option.None<TValue>(), cancellationToken);

        public static ValueTask SetAsync<TKey, TValue>(this ICache<TKey, TValue> cache, 
            TKey key, TValue value, CancellationToken cancellationToken = default)
            where TKey : notnull
            => cache.SetAsync(key, Option.Some(value), cancellationToken);
    }
}
