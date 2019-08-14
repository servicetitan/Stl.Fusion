using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Optional;
using Optional.Unsafe;
using Stl.Collections;

namespace Stl.Collections
{
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