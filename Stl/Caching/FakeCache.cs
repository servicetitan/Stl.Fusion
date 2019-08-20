using System.Threading;
using System.Threading.Tasks;
using Optional;
using Stl.Async;

namespace Stl.Caching 
{
    public class FakeCache<TKey, TValue> : CacheBase<TKey, TValue>
        where TKey : notnull
    {
        public override ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
            => ValueTaskEx.New(Option.None<TValue>());

        protected override ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default) 
            => ValueTaskEx.CompletedTask;
    }
}
