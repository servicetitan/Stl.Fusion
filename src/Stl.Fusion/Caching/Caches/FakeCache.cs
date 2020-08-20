using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    public class FakeCache<TKey, TValue> : ICache<TKey, TValue>
        where TKey : notnull
    {
        public ValueTask SetAsync(TKey key, TValue value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public virtual ValueTask<Option<TValue>> GetAsync(TKey key, CancellationToken cancellationToken)
            => ValueTaskEx.FromResult(Option.None<TValue>());
    }
}
