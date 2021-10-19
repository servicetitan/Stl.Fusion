using Stl.Internal;

namespace Stl.Caching;

public abstract class FastComputingCacheBase<TKey, TValue> : AsyncKeyResolverBase<TKey, TValue>
    where TKey : notnull
{
    protected AsyncLocal<ImmutableHashSet<TKey>> AccessedKeys { get; set; } = new();
    protected ConcurrentDictionary<TKey, (Task<TValue> Task, Result<TValue> Result)> Cache { get; set; } = new();

    protected abstract ValueTask<TValue> Compute(TKey key, CancellationToken cancellationToken = default);

    public override ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default)
    {
        if (Cache.TryGetValue(key, out var cached))
            return Unwrap(cached);
        return Unwrap(Cache.GetOrAdd(key,
            (k, s) => (ComputeAndUpdate(s.self, k, s.cancellationToken), default)!,
            (self: this, cancellationToken)));
    }

    public override async ValueTask<Option<TValue>> TryGet(TKey key, CancellationToken cancellationToken = default)
    {
        var value = await Get(key, cancellationToken).ConfigureAwait(false);
        return Option.Some(value);
    }

    protected async ValueTask<TValue> SafeCompute(TKey key, CancellationToken cancellationToken = default)
    {
        var accessedKeys = AccessedKeys.Value;
        if (accessedKeys?.Contains(key) ?? false)
            throw Errors.CircularDependency(key);
        AccessedKeys.Value = (accessedKeys ?? ImmutableHashSet<TKey>.Empty).Add(key);
        try {
            return await Compute(key, cancellationToken).ConfigureAwait(false);
        }
        finally {
            AccessedKeys.Value = AccessedKeys.Value?.Remove(key) ?? ImmutableHashSet<TKey>.Empty;
        }
    }

    private static ValueTask<TValue> Unwrap((Task<TValue>? Task, Result<TValue> Result) entry)
        => entry.Task?.ToValueTask() ?? entry.Result.AsValueTask();

    private static async Task<TValue> ComputeAndUpdate(
        FastComputingCacheBase<TKey, TValue> self,
        TKey key,
        CancellationToken cancellationToken)
    {
        var r = await self.SafeCompute(key, cancellationToken).ConfigureAwait(false);
        self.Cache[key] = (null, r)!;
        return r;
    }
}

public class FastComputingCache<TKey, TValue> : FastComputingCacheBase<TKey, TValue>
    where TKey : notnull
{
    private Func<TKey, CancellationToken, ValueTask<TValue>> Computer { get; }

    public FastComputingCache(Func<TKey, CancellationToken, ValueTask<TValue>> computer)
        => Computer = computer;

    protected override ValueTask<TValue> Compute(TKey key, CancellationToken cancellationToken = default)
        => Computer.Invoke(key, cancellationToken);
}
