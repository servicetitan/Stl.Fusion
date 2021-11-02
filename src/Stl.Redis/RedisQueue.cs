using StackExchange.Redis;

namespace Stl.Redis;

public class RedisQueue<T> : IAsyncDisposable
{
    public record Options
    {
        public string EnqueuePubSubKeySuffix { get; init; } = "-updates";
        public TimeSpan DequeueTimeout { get; init; } = TimeSpan.FromSeconds(0.250);
        public IByteSerializer<T> Serializer { get; init; } = ByteSerializer<T>.Default;
        public IMomentClock Clock { get; init; } = CpuClock.Instance;
    }

    public Options Settings { get; }
    public RedisDb RedisDb { get; }
    public string Key { get; }
    public RedisPubSub EnqueuePubSub { get; }

    public RedisQueue(RedisDb redisDb, string key, Options? settings = null)
    {
        Settings = settings ?? new();
        RedisDb = redisDb;
        Key = key;
        EnqueuePubSub = RedisDb.GetPubSub($"{typeof(T).Name}-{Key}{Settings.EnqueuePubSubKeySuffix}");
    }

    public ValueTask DisposeAsync()
        => EnqueuePubSub.DisposeAsync();

    public async Task Enqueue(T item)
    {
        using var bufferWriter = Settings.Serializer.Writer.Write(item);
        await RedisDb.Database.ListLeftPushAsync(Key, bufferWriter.WrittenMemory).ConfigureAwait(false);
        await EnqueuePubSub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
    }

    public async Task<T> Dequeue(CancellationToken cancellationToken = default)
    {
        while (true) {
            var value = await RedisDb.Database.ListRightPopAsync(Key).ConfigureAwait(false);
            if (!value.IsNullOrEmpty)
                return Settings.Serializer.Reader.Read(value);
            try {
                await EnqueuePubSub.Read(cancellationToken).AsTask()
                    .WithTimeout(Settings.Clock, Settings.DequeueTimeout, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ChannelClosedException) {
                // Delay is highly desirable here, otherwise we might end up
                // getting tons of exceptions thrown & caught
                await Settings.Clock.Delay(Settings.DequeueTimeout, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task Remove()
        => RedisDb.Database.KeyDeleteAsync(Key, CommandFlags.FireAndForget);
}
