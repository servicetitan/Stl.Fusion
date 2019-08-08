using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Optional;
using Optional.Unsafe;

namespace Stl.Caching
{
    public abstract class ReadOnlyCacheBase<TKey, TValue> : IReadOnlyCache<TKey, TValue>
        where TKey : notnull
    {
        public virtual async ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var value = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (!value.HasValue)
                throw new KeyNotFoundException();
            return value.ValueOrDefault();
        }

        public abstract ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default);
    }
    
    public abstract class CacheBase<TKey, TValue> : ReadOnlyCacheBase<TKey, TValue>, ICache<TKey, TValue>
        where TKey : notnull
    {
        public ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
            => SetAsync(key, Option.Some(value), cancellationToken);
        
        public ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
            => SetAsync(key, Option.None<TValue>(), cancellationToken);
        
        protected abstract ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default);
    }
}