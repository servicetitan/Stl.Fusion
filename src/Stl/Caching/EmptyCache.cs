using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Caching
{
    public class EmptyCache<TKey, TValue> : AsyncCacheBase<TKey, TValue>
        where TKey : notnull
    {
        public override ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
            => ValueTaskEx.FromResult(Option.None<TValue>());

        protected override ValueTask Set(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
            => ValueTaskEx.CompletedTask;
    }
}
