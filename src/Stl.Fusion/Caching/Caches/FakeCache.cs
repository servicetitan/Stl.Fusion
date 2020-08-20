using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Caching
{
    public class FakeCache<TKey> : ICache<TKey>
        where TKey : notnull
    {
        public ValueTask SetAsync(TKey key, object value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public virtual ValueTask<Option<object>> GetAsync(TKey key, CancellationToken cancellationToken)
            => ValueTaskEx.FromResult(Option.None<object>());
    }
}
