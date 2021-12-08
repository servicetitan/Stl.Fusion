using Stl.Mathematics;

namespace Stl.Redis;

public class RedisSequenceSet
{
    public RedisHash Hash { get; }
    public Range<long> ResetRange { get; init; } = (-100, 100);

    public RedisSequenceSet(RedisHash hash)
        => Hash = hash;

    public async Task<long> Next(string key, long maxUsedValue = 0, long increment = 1)
    {
        while (true) {
            var value = await Hash.Increment(key, increment).ConfigureAwait(false);
            if (ResetRange.Move(maxUsedValue).Contains(value))
                return value;
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
    public RedisSequenceSet(RedisHash hash) : base(hash) { }
}
