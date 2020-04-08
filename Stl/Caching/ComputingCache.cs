using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Locking;

namespace Stl.Caching 
{
    public abstract class ComputingCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>
        where TKey : notnull
    {
        public IAsyncCache<TKey, TValue> Cache { get; }
        public IAsyncLockSet<TKey> Locks { get; }

        protected ComputingCacheBase(IAsyncCache<TKey, TValue> cache, IAsyncLockSet<TKey>? lockSet = null)
        {
            Cache = cache;
            Locks = lockSet ?? new AsyncLockSet<TKey>(ReentryMode.CheckedFail);
        }

        public override async ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            // Read-Lock-RetryRead-Compute-Store pattern

            var valueOpt = await Cache.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (valueOpt.IsSome(out var value))
                return value;

            using var @lock = await Locks.LockAsync(key, cancellationToken).ConfigureAwait(false);
            
            valueOpt = await Cache.TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (valueOpt.IsSome(out value))
                return value;
            
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

        public ComputingCache(IAsyncCache<TKey, TValue> cache, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache) 
            => Computer = computer;

        public ComputingCache(IAsyncCache<TKey, TValue> cache, IAsyncLockSet<TKey> lockSet, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache, lockSet) 
            => Computer = computer;

        protected override ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default) 
            => Computer.Invoke(key, cancellationToken);
    }
}
