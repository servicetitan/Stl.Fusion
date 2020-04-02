using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Locking;

namespace Stl.Caching 
{
    public abstract class ComputingCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>
        where TKey : notnull
    {
        public ICache<TKey, TValue> Cache { get; }
        public IAsyncLockSet<TKey> LockSet { get; }

        protected ComputingCacheBase(ICache<TKey, TValue> cache, IAsyncLockSet<TKey>? lockSet = null)
        {
            Cache = cache;
            LockSet = lockSet ?? new AsyncLockSet<TKey>(ReentryMode.CheckedFail);
        }

        public override async ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var value = await Cache.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (value.HasValue)
                return value.UnsafeValue;
            using var @lock = await LockSet.LockAsync(key, cancellationToken).ConfigureAwait(false);
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

        public ComputingCache(ICache<TKey, TValue> cache, IAsyncLockSet<TKey> lockSet, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache, lockSet) 
            => Computer = computer;

        protected override ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default) 
            => Computer.Invoke(key, cancellationToken);
    }
}
