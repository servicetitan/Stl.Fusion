using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisActionSub : RedisSubBase
{
    private Action<RedisChannel, RedisValue> MessageHandler { get; }

    public RedisActionSub(RedisDb redisDb, RedisSubKey key,
        Action<RedisChannel, RedisValue> messageHandler,
        TimeSpan? subscribeTimeout = null)
        : base(redisDb, key, subscribeTimeout)
        => MessageHandler = messageHandler;

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
        => MessageHandler(redisChannel, redisValue);
}

public sealed class RedisActionSub<T> : RedisSubBase
{
    private Action<RedisChannel, T> MessageHandler { get; }

    public IByteSerializer<T> Serializer { get; }

    public RedisActionSub(RedisDb redisDb,
        RedisSubKey key,
        Action<RedisChannel, T> messageHandler,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
        : base(redisDb, key, subscribeTimeout)
    {
        MessageHandler = messageHandler;
        Serializer = serializer ?? ByteSerializer<T>.Default;
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        var value = Serializer.Read(redisValue);
        MessageHandler(redisChannel, value);
    }
}
