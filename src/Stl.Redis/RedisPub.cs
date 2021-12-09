using StackExchange.Redis;

namespace Stl.Redis;

public class RedisPub
{
    public RedisDb RedisDb { get; }
    public string Key { get; }
    public string FullKey { get; }
    private ISubscriber Subscriber { get; }

    public RedisPub(RedisDb redisDb, string key)
    {
        RedisDb = redisDb;
        Key = key;
        FullKey = RedisDb.FullKey(Key);
        Subscriber = RedisDb.Redis.GetSubscriber();
    }

    public Task<long> Publish(RedisValue item)
        => Subscriber.PublishAsync(FullKey, item);
}

public sealed class RedisPub<T> : RedisPub
{
    public IByteSerializer<T> Serializer { get; }

    public RedisPub(RedisDb redisDb, string key, IByteSerializer<T>? serializer = null)
        : base(redisDb, key)
        => Serializer = serializer ?? ByteSerializer<T>.Default;

    public async Task<long> Publish(T item)
    {
        using var bufferWriter = Serializer.Writer.Write(item);
        return await base.Publish(bufferWriter.WrittenMemory).ConfigureAwait(false);
    }
}
