using System.Text;
using StackExchange.Redis;

namespace Stl.Redis;

public class RedisPub
{
    public RedisDb RedisDb { get; }
    public string Key { get; }
    public string FullKey { get; }
    public RedisChannel Channel { get; }
    private ISubscriber Subscriber { get; }

    public RedisPub(RedisDb redisDb, string key)
    {
        RedisDb = redisDb;
        Key = key;
        FullKey = RedisDb.FullKey(Key);
        Channel = new RedisChannel(Encoding.UTF8.GetBytes(FullKey), RedisChannel.PatternMode.Auto);
        Subscriber = RedisDb.Redis.GetSubscriber();
    }

    public Task<long> Publish(RedisValue item)
        => Subscriber.PublishAsync(Channel, item);
}

public sealed class RedisPub<T> : RedisPub
{
    public IByteSerializer<T> Serializer { get; }

    public RedisPub(RedisDb redisDb, string key, IByteSerializer<T>? serializer = null)
        : base(redisDb, key)
        => Serializer = serializer ?? ByteSerializer<T>.Default;

    public async Task<long> Publish(T item)
    {
        using var bufferWriter = Serializer.Write(item);
        return await base.Publish(bufferWriter.WrittenMemory).ConfigureAwait(false);
    }
}
