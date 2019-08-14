using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Optional;
using Optional.Unsafe;

namespace Stl.Collections
{
    public interface IAsyncKeyResolver<in TKey, TValue>
        where TKey : notnull
    {
        ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default);
        ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public abstract class AsyncKeyResolverBase<TKey, TValue> : IAsyncKeyResolver<TKey, TValue>
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
}