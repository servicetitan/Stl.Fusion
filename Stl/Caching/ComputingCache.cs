using System;
using System.Threading;
using System.Threading.Tasks;
using Optional.Unsafe;
using Stl.Locking;

namespace Stl.Caching 
{
    public interface IComputingCache<in TKey, TValue>
    {
        ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default);
        ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public abstract class ComputingCacheBase<TKey, TValue> : IComputingCache<TKey, TValue>
        where TKey : notnull
    {
        protected ICache<TKey, TValue> Cache { get; }
        protected ILock<TKey> Lock { get; }

        protected ComputingCacheBase(ICache<TKey, TValue> cache)
        {
            Cache = cache;
            Lock = new InProcessLock<TKey>();
        }

        protected ComputingCacheBase(ICache<TKey, TValue> cache, ILock<TKey> @lock)
        {
            Cache = cache;
            Lock = @lock ?? new InProcessLock<TKey>();
        }

        public async ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default)
        {
            var value = await Cache.GetAsync(key, cancellationToken);
            if (value.HasValue)
                return value.ValueOrDefault();
            await using var @lock = await Lock.LockAsync(key, cancellationToken);
            var result = await ComputeAsync(key, cancellationToken);
            await Cache.SetAsync(key, result, cancellationToken);
            return result;
        }

        public ValueTask InvalidateAsync(TKey key, CancellationToken cancellationToken = default) 
            => Cache.InvalidateAsync(key, cancellationToken);

        protected abstract ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default);
    }

    public class ComputingCache<TKey, TValue> : ComputingCacheBase<TKey, TValue>
        where TKey : notnull
    {
        private Func<TKey, CancellationToken, ValueTask<TValue>> Computer { get; }

        public ComputingCache(ICache<TKey, TValue> cache, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache) 
            => Computer = computer;

        public ComputingCache(ICache<TKey, TValue> cache, ILock<TKey> @lock, Func<TKey, CancellationToken, ValueTask<TValue>> computer) 
            : base(cache, @lock) 
            => Computer = computer;

        protected override ValueTask<TValue> ComputeAsync(TKey key, CancellationToken cancellationToken = default) 
            => Computer.Invoke(key, cancellationToken);
    }
}
