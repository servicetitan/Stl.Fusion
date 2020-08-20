using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    public class FakeCache : ICache
    {
        public ValueTask SetAsync(InterceptedInput key, Result<object> value, TimeSpan expirationTime, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public ValueTask RemoveAsync(InterceptedInput key, CancellationToken cancellationToken)
        {
            Computed.Invalidate(() => GetAsync(key, default));
            return ValueTaskEx.CompletedTask;
        }

        public virtual ValueTask<Option<Result<object>>> GetAsync(InterceptedInput key, CancellationToken cancellationToken)
            => ValueTaskEx.FromResult(Option.None<Result<object>>());
    }
}
