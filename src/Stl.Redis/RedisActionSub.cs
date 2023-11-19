using System.Diagnostics.CodeAnalysis;
using StackExchange.Redis;
using Stl.Internal;

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

public sealed class RedisActionSub<T>(RedisDb redisDb,
        RedisSubKey key,
        Action<RedisChannel, T> messageHandler,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
    : RedisSubBase(redisDb, key, subscribeTimeout)
{
    private Action<RedisChannel, T> MessageHandler { get; } = messageHandler;

    public IByteSerializer<T> Serializer { get; } = serializer ?? ByteSerializer<T>.Default;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
#pragma warning restore IL2046
    {
        var value = Serializer.Read(redisValue);
        MessageHandler(redisChannel, value);
    }
}
