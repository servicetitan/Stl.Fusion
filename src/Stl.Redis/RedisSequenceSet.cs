using Stl.Mathematics;

namespace Stl.Redis;

public class RedisSequenceSet
{
    public RedisHash Hash { get; }
    public long ResetRange { get; init; } = 1024;

    public RedisSequenceSet(RedisHash hash)
        => Hash = hash;

    public async Task<long> Next(string key, long maxUsedValue = -1, long increment = 1)
    {
        var value = await Hash.Increment(key, increment).ConfigureAwait(false);
        if (maxUsedValue < 0)
            return value;
        if (maxUsedValue < value && value <= maxUsedValue + ResetRange)
            return value;
        value = maxUsedValue + increment;
        await Reset(key, value).ConfigureAwait(false);
        return value;
    }

    public Task Reset(string key, long value)
        => Hash.Set(key, value);

    public Task Clear()
        => Hash.Clear();
}

public sealed class RedisSequenceSet<TScope> : RedisSequenceSet
{
    public RedisSequenceSet(RedisHash hash) : base(hash) { }
}
