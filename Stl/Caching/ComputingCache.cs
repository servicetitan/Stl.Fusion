using System;
using System.Threading;
using System.Threading.Tasks;
using Optional;
using Optional.Unsafe;
using Stl.Collections;
using Stl.Locking;

namespace Stl.Caching 
{
    public abstract class ComputingCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>
        where TKey : notnull
    {
        public ICache<TKey, TValue> Cache { get; }
        public IAsyncLock<TKey> Lock { get; }

        protected ComputingCacheBase(ICache<TKey, TValue> cache, IAsyncLock<TKey>? @lock = null)
        {
            Cache = cache;
            Lock = @lock ?? new AsyncLock<TKey>(ReentryMode.CheckedFail);
        }

        public override async ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var value = await Cache.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (value.HasValue)
                return value.ValueOrDefault();
            await using var @lock = await Lock.LockAsync(key, cancellationToken).ConfigureAwait(false);
            var result = await ComputeAsync(key, cancellationToken).ConfigureAwait(false);
            await Cache.SetAsync(key, result, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async ValueTask<Option<TValue>> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var value = await GetAsync(key, cancellationToken).ConfigureAwait(false);
            return Option.Some(value);
        }
        
        protected abstract ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public class ComputingCache<TKey, TValue> : ComputingCacheBase<TKey, TValue>
        where TKey : notnull
    {
        private Func<TKey, CancellationToken, ValueTask<TValue>> Computer { get; }

        public ComputingCache(ICache<TKey, TValue> cache, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache) 
            => Computer = computer;

        public ComputingCache(ICache<TKey, TValue> cache, IAsyncLock<TKey> @lock, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache, @lock) 
            => Computer = computer;

        protected override ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default) 
            => Computer.Invoke(key, cancellationToken);
    }
}
