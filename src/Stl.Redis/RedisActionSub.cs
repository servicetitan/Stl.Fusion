using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisActionSub : RedisSubBase
{
    private Action<RedisChannel, RedisValue> MessageHandler { get; }

    public RedisActionSub(RedisDb redisDb, string key,
        Action<RedisChannel, RedisValue> messageHandler,
        RedisChannel.PatternMode patternMode = RedisChannel.PatternMode.Auto)
        : base(redisDb, key, patternMode)
        => MessageHandler = messageHandler;

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
        => MessageHandler(redisChannel, redisValue);
}

public sealed class RedisActionSub<T> : RedisSubBase
{
    private Action<RedisChannel, T> MessageHandler { get; }

    public IByteSerializer<T> Serializer { get; }

    public RedisActionSub(RedisDb redisDb, string key,
        Action<RedisChannel, T> messageHandler,
        IByteSerializer<T>? serializer = null,
        RedisChannel.PatternMode patternMode = RedisChannel.PatternMode.Auto)
        : base(redisDb, key, patternMode)
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
