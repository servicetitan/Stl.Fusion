using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public abstract class CacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>, ICache<TKey, TValue>
        where TKey : notnull
    {
        public ValueTask SetAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
            => SetAsync(key, Option.Some(value), cancellationToken);
        
        public ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default)
            => SetAsync(key, Option.None<TValue>(), cancellationToken);
        
        protected abstract ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default);
    }
}
