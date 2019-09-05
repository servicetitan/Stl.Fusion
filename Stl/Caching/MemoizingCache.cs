using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Caching
{
    public class MemoizingCache<TKey, TValue> : CacheBase<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();

        public override ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
            => ValueTaskEx.New(_dictionary[key]);

        public override ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
            => ValueTaskEx.New(
                _dictionary.TryGetValue(key, out var value) 
                    ? Option.Some(value) 
                    : default);

        protected override ValueTask SetAsync(TKey key, Option<TValue> value, CancellationToken cancellationToken = default)
        {
            if (value.HasValue)
                _dictionary[key] = value.UnsafeValue;
            else
                _dictionary.TryRemove(key, out _);
            return ValueTaskEx.CompletedTask;
        }
    }
}
