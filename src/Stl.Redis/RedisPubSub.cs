using StackExchange.Redis;

namespace Stl.Redis;

public class RedisPubSub : IAsyncDisposable
{
    private ISubscriber? _subscriber;
    private Task<ChannelMessageQueue>? _queueTask;
    private readonly object _lock = new();

    public RedisDb RedisDb { get; }
    public string Key { get; }
    public string FullKey { get; }
    public ISubscriber Subscriber => _subscriber ??= RedisDb.Redis.GetSubscriber();

    public RedisPubSub(RedisDb redisDb, string key)
    {
        RedisDb = redisDb;
        Key = key;
        FullKey = RedisDb.FullKey(Key);
    }

    public ValueTask DisposeAsync()
        => Reset();

    public ValueTask<ChannelMessageQueue> GetQueue()
    {
        var queueTask = _queueTask;
        if (queueTask == null) lock (_lock) {
            queueTask = _queueTask ??= Subscriber.SubscribeAsync(FullKey);
        }
        return queueTask.ToValueTask();
    }

    public Task<long> Publish(RedisValue item)
        => Subscriber.PublishAsync(FullKey, item);

    public async ValueTask<RedisValue> Read(CancellationToken cancellationToken = default)
    {
        try {
            var queue = await GetQueue().ConfigureAwait(false);
            var message = await queue.ReadAsync(cancellationToken).ConfigureAwait(false);
            return message.Message;
        }
        catch (Exception) {
            _ = Reset();
            throw;
        }
    }

    public async ValueTask<Option<RedisValue>> TryRead(CancellationToken cancellationToken = default)
    {
        try {
            return await Read(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException) {
            return Option<RedisValue>.None;
        }
    }

    // Protected methods

    protected async ValueTask Reset()
    {
        Task<ChannelMessageQueue>? queueTask;
        lock (_lock) {
            queueTask = _queueTask;
            _queueTask = null;
        }
        if (queueTask == null)
            return;
        try {
            var queue = await queueTask.ConfigureAwait(false);
            await queue.UnsubscribeAsync().ConfigureAwait(false);
        }
        catch {
            // Intended
        }
    }
}

public class RedisPubSub<T> : RedisPubSub
{
    public IByteSerializer<T> Serializer { get; }

    public RedisPubSub(RedisDb redisDb, string key, ByteSerializer<T>? serializer = null)
        : base(redisDb, key)
        => Serializer = serializer ?? ByteSerializer<T>.Default;

    public Task<long> PublishRaw(RedisValue item)
        => base.Publish(item);
    public ValueTask<RedisValue> ReadRaw(CancellationToken cancellationToken = default)
        => base.Read(cancellationToken);
    public ValueTask<Option<RedisValue>> TryReadRaw(CancellationToken cancellationToken = default)
        => base.TryRead(cancellationToken);

    public async Task<long> Publish(T item)
    {
        using var bufferWriter = Serializer.Writer.Write(item);
        return await base.Publish(bufferWriter.WrittenMemory).ConfigureAwait(false);
    }

    public new async ValueTask<T> Read(CancellationToken cancellationToken = default)
    {
        var value = await base.Read(cancellationToken).ConfigureAwait(false);
        return Serializer.Reader.Read(value);
    }

    public new async ValueTask<Option<T>> TryRead(CancellationToken cancellationToken = default)
    {
        try {
            return await Read(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException) {
            return Option<T>.None;
        }
    }
}
