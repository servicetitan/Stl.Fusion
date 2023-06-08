using Stl.Locking;

namespace Stl.Caching;

public abstract class ComputingCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>
    where TKey : notnull
{
    public IAsyncCache<TKey, TValue> Cache { get; }
    public AsyncLockSet<TKey> Locks { get; }

    protected ComputingCacheBase(IAsyncCache<TKey, TValue> cache, AsyncLockSet<TKey>? lockSet = null)
    {
        Cache = cache;
        Locks = lockSet ?? new AsyncLockSet<TKey>(LockReentryMode.CheckedFail);
    }

    // Note that Get is the primary method here, not TryGet -
    // that's because computing cache _always_ produces a result,
    // i.e. there is no "miss" concept
    public override async ValueTask<TValue?> Get(TKey key, CancellationToken cancellationToken = default)
    {
        // Read-Lock-RetryRead-Compute-Store pattern;

        var valueOpt = await Cache.TryGet(key, cancellationToken).ConfigureAwait(false);
        if (valueOpt.IsSome(out var value))
            return value;

        using var @lock = await Locks.Lock(key, cancellationToken).ConfigureAwait(false);

        valueOpt = await Cache.TryGet(key, cancellationToken).ConfigureAwait(false);
        if (valueOpt.IsSome(out value))
            return value;

        var result = await Compute(key, cancellationToken).ConfigureAwait(false);
        await Cache.Set(key, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public sealed override async ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
    {
        var value = await Get(key, cancellationToken).ConfigureAwait(false);
        return Option.Some(value!);
    }

    protected abstract ValueTask<TValue> Compute(TKey key, CancellationToken cancellationToken = default);
}

public class ComputingCache<TKey, TValue> : ComputingCacheBase<TKey, TValue>
    where TKey : notnull
{
    private Func<TKey, CancellationToken, ValueTask<TValue>> Computer { get; }

    public ComputingCache(IAsyncCache<TKey, TValue> cache, Func<TKey, CancellationToken, ValueTask<TValue>> computer)
        : base(cache)
        => Computer = computer;

    public ComputingCache(IAsyncCache<TKey, TValue> cache, AsyncLockSet<TKey> lockSet, Func<TKey, CancellationToken, ValueTask<TValue>> computer)
        : base(cache, lockSet)
        => Computer = computer;

    protected override ValueTask<TValue> Compute(TKey key, CancellationToken cancellationToken = default)
        => Computer(key, cancellationToken);
}
