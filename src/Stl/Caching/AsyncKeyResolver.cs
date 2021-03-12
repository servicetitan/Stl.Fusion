using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Caching
{
    public abstract class AsyncKeyResolverBase<TKey, TValue> : IAsyncKeyResolver<TKey, TValue>
        where TKey : notnull
    {
        public virtual async ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default)
        {
            var maybeValue = await TryGet(key, cancellationToken).ConfigureAwait(false);
            if (maybeValue.IsSome(out var value))
                return value;
            throw new KeyNotFoundException();
        }

        public abstract ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default);
    }
}
