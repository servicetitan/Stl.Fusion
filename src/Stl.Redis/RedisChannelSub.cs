using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisChannelSub : RedisSubBase
{
    private readonly Channel<RedisValue> _channel;

    public ChannelReader<RedisValue> Messages => _channel.Reader;

    public RedisChannelSub(RedisDb redisDb, string key,
        Channel<RedisValue>? channel = null)
        : base(redisDb, key)
        => _channel = channel ?? Channel.CreateUnbounded<RedisValue>(
            new UnboundedChannelOptions() {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
            });

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
        => _channel.Writer.TryWrite(redisValue);
}

public sealed class RedisChannelSub<T> : RedisSubBase
{
    private readonly Channel<T> _channel;

    public IByteSerializer<T> Serializer { get; }
    public ChannelReader<T> Messages => _channel.Reader;

    public RedisChannelSub(RedisDb redisDb, string key,
        Channel<T>? channel = null,
        IByteSerializer<T>? serializer = null)
        : base(redisDb, key)
    {
        Serializer = serializer ?? ByteSerializer<T>.Default;
        _channel = channel ?? Channel.CreateUnbounded<T>(
            new UnboundedChannelOptions() {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
            });
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        try {
            var value = Serializer.Read(redisValue);
            _channel.Writer.TryWrite(value);
        }
        catch (Exception e) {
            _channel.Writer.TryComplete(e);
        }
    }
}
