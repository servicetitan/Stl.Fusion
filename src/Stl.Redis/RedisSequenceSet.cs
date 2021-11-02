namespace Stl.Redis;

public class RedisSequenceSet
{
    public RedisHash Hash { get; }
    public long AutoResetDistance { get; }

    public RedisSequenceSet(RedisHash hash, long autoResetDistance = 10)
    {
        Hash = hash;
        AutoResetDistance = autoResetDistance;
    }

    public async Task<long> Next(string key, long maxUsedValue = 0, long increment = 1)
    {
        while (true) {
            var value = await Hash.Increment(key, increment).ConfigureAwait(false);
            if (maxUsedValue < value)
                return value;
            if (maxUsedValue - value >= AutoResetDistance)
                await Reset(key, maxUsedValue).ConfigureAwait(false);
        }
    }

    public Task Reset(string key, long value)
        => Hash.Set(key, value);

    public Task Clear()
        => Hash.Clear();
}

public class RedisSequenceSet<TScope> : RedisSequenceSet
{
    public RedisSequenceSet(RedisHash hash, long autoResetDistance = 10)
        : base(hash, autoResetDistance) { }
}
