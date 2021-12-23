using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisQueue : IAsyncDisposable
{
    public record Options
    {
        public string EnqueuePubKeySuffix { get; init; } = "-updates";
        public TimeSpan EnqueueCheckPeriod { get; init; } = TimeSpan.FromSeconds(1);
        public IMomentClock Clock { get; init; } = MomentClockSet.Default.CpuClock;
    }

    private RedisPub EnqueuePub { get; }
    private RedisTaskSub EnqueueSub { get; }

    public Options Settings { get; }
    public RedisDb RedisDb { get; }
    public string Key { get; }

    public RedisQueue(RedisDb redisDb, string key, Options? settings = null)
    {
        Settings = settings ?? new();
        RedisDb = redisDb;
        Key = key;
        var enqueuePubKey = $"{Key}{Settings.EnqueuePubKeySuffix}";
        EnqueuePub = RedisDb.GetPub(enqueuePubKey);
        EnqueueSub = RedisDb.GetTaskSub(enqueuePubKey);
    }

    public ValueTask DisposeAsync()
        => EnqueueSub.DisposeAsync();

    public async Task Enqueue(RedisValue redisValue)
    {
        if (redisValue.IsNullOrEmpty)
            throw new ArgumentOutOfRangeException(nameof(redisValue));
        await RedisDb.Database.ListLeftPushAsync(Key, redisValue).ConfigureAwait(false);
        await EnqueuePub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
    }

    public async Task<RedisValue> Dequeue(CancellationToken cancellationToken = default)
    {
        while (true) {
            var nextEnqueueNotificationTask = EnqueueSub.NextMessage();
            var redisValue = await RedisDb.Database.ListRightPopAsync(Key).ConfigureAwait(false);
            if (!redisValue.IsNullOrEmpty)
                return redisValue;
            await nextEnqueueNotificationTask
                .WithTimeout(Settings.Clock, Settings.EnqueueCheckPeriod, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task Remove()
        => RedisDb.Database.KeyDeleteAsync(Key, CommandFlags.FireAndForget);
}

public sealed class RedisQueue<T> : IAsyncDisposable
{
    public record Options
    {
        public string EnqueuePubKeySuffix { get; init; } = "-updates";
        public TimeSpan EnqueueCheckPeriod { get; init; } = TimeSpan.FromSeconds(1);
        public IByteSerializer<T> Serializer { get; init; } = ByteSerializer<T>.Default;
        public IMomentClock Clock { get; init; } = MomentClockSet.Default.CpuClock;
    }

    private RedisPub EnqueuePub { get; }
    private RedisTaskSub EnqueueSub { get; }

    public Options Settings { get; }
    public RedisDb RedisDb { get; }
    public string Key { get; }

    public RedisQueue(RedisDb redisDb, string key, Options? settings = null)
    {
        Settings = settings ?? new();
        RedisDb = redisDb;
        Key = key;
        var enqueuePubKey = $"{typeof(T).Name}-{Key}{Settings.EnqueuePubKeySuffix}";
        EnqueuePub = RedisDb.GetPub(enqueuePubKey);
        EnqueueSub = RedisDb.GetTaskSub(enqueuePubKey);
    }

    public ValueTask DisposeAsync()
        => EnqueueSub.DisposeAsync();

    public async Task Enqueue(T item)
    {
        using var bufferWriter = Settings.Serializer.Write(item);
        await RedisDb.Database.ListLeftPushAsync(Key, bufferWriter.WrittenMemory).ConfigureAwait(false);
        await EnqueuePub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
    }

    public async Task<T> Dequeue(CancellationToken cancellationToken = default)
    {
        while (true) {
            var nextEnqueueNotificationTask = EnqueueSub.NextMessage();
            var value = await RedisDb.Database.ListRightPopAsync(Key).ConfigureAwait(false);
            if (!value.IsNullOrEmpty)
                return Settings.Serializer.Read(value);
            await nextEnqueueNotificationTask
                .WithTimeout(Settings.Clock, Settings.EnqueueCheckPeriod, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public Task Remove()
        => RedisDb.Database.KeyDeleteAsync(Key, CommandFlags.FireAndForget);
}
