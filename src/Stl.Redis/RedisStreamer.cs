using System.Diagnostics.CodeAnalysis;
using StackExchange.Redis;
using Stl.Internal;
using Errors = Stl.Redis.Internal.Errors;

namespace Stl.Redis;

public sealed class RedisStreamer<T>(RedisDb redisDb, string key, RedisStreamer<T>.Options? settings = null)
{
    public record Options
    {
        public int MaxStreamLength { get; init; } = 2048;
        public string AppendPubKeySuffix { get; init; } = "-updates";
        public TimeSpan AppendCheckPeriod { get; init; } = TimeSpan.FromSeconds(1);
        public TimeSpan? AppendSubscribeTimeout { get; init; } = TimeSpan.FromSeconds(5);
        public TimeSpan? ExpirationPeriod { get; set; } = TimeSpan.FromHours(1);
        public IByteSerializer<T> Serializer { get; init; } = ByteSerializer<T>.Default;
        public ITextSerializer<ExceptionInfo> ErrorSerializer { get; init; } = TextSerializer<ExceptionInfo>.Default;
        public IMomentClock Clock { get; init; } = MomentClockSet.Default.CpuClock;

        // You normally don't need to modify these
        public string ItemKey { get; init; } = "i";
        public string StatusKey { get; init; } = "s";
        public string StartedStatus { get; init; } = "[";
        public string EndedStatus { get; init; } = "]";
    }

    public Options Settings { get; } = settings ?? new ();
    public RedisDb RedisDb { get; } = redisDb;
    public string Key { get; } = key;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public async IAsyncEnumerable<T> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var appendSub = GetAppendSub();
        await using var _ = appendSub.ConfigureAwait(false);
        await appendSub.Subscribe().ConfigureAwait(false);

        var position = (RedisValue)"0-0";
        var serializer = Settings.Serializer;
        var appendNotificationTask = appendSub.NextMessage();
        while (true) {
            cancellationToken.ThrowIfCancellationRequested(); // Redis doesn't support cancellation
            var entries = await RedisDb.Database.StreamReadAsync(Key, position, 10).ConfigureAwait(false);
            if (entries == null! || entries.Length == 0) {
                var appendResult = await appendNotificationTask
                    .WaitResultAsync(Settings.Clock, Settings.AppendCheckPeriod, cancellationToken)
                    .ConfigureAwait(false);
                if (appendResult.HasValue)
                    appendNotificationTask = null;
                appendNotificationTask = appendSub.NextMessage(appendNotificationTask);
                continue;
            }

            foreach (var entry in entries) {
                var status = (string?)entry[Settings.StatusKey];
                if (!status.IsNullOrEmpty()) {
                    if (StringComparer.Ordinal.Equals(status, Settings.StartedStatus))
                        continue;
                    if (StringComparer.Ordinal.Equals(status, Settings.EndedStatus))
                        yield break;
                    var errorInfo = Settings.ErrorSerializer.Read(status!);
                    throw errorInfo.ToException() ?? Errors.SourceStreamError();
                }

                var data = (ReadOnlyMemory<byte>)entry[Settings.ItemKey];
                var item = serializer.Read(data);
                yield return item;

                position = entry.Id;
            }
        }
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task Write(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
        => Write(source, _ => default, cancellationToken);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public Task Write(
        IAsyncEnumerable<T> source,
        Action<RedisStreamer<T>> newStreamAnnouncer,
        CancellationToken cancellationToken = default)
        => Write(source,
            self => {
                newStreamAnnouncer(self);
                return default;
            },
            cancellationToken);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public async Task Write(
        IAsyncEnumerable<T> source,
        Func<RedisStreamer<T>, ValueTask> newStreamAnnouncer,
        CancellationToken cancellationToken = default)
    {
        var appendPub = GetAppendPub();
        var error = (Exception?) null;
        var lastAppendTask = AppendStart(newStreamAnnouncer, appendPub, cancellationToken);
        if (Settings.ExpirationPeriod is { } expirationPeriod)
            await RedisDb.Database.KeyExpireAsync(Key, expirationPeriod).ConfigureAwait(false);
        try {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                await lastAppendTask.ConfigureAwait(false);
                lastAppendTask = AppendItem(item, appendPub, cancellationToken);
            }
            await lastAppendTask.ConfigureAwait(false);
        }
        catch (Exception e) {
            error = e;
        }
        finally {
            if (!lastAppendTask.IsCompleted)
                try {
                    await lastAppendTask.ConfigureAwait(false);
                }
                catch (Exception e) {
                    error = e;
                }
            // No cancellation for AppendEnd - it should propagate it
            await AppendEnd(error, appendPub).ConfigureAwait(false);
        }
        if (error != null)
            throw error;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task AppendStart(
        Func<RedisStreamer<T>, ValueTask> newStreamAnnouncer,
        RedisPub appendPub,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // StackExchange.Redis doesn't support cancellation
        await RedisDb.Database.StreamAddAsync(
                Key, Settings.StatusKey, Settings.StartedStatus,
                maxLength: Settings.MaxStreamLength,
                useApproximateMaxLength: true)
            .ConfigureAwait(false);
        await appendPub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
        await newStreamAnnouncer(this).ConfigureAwait(false);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async Task AppendItem(
        T item,
        RedisPub appendPub,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // StackExchange.Redis doesn't support cancellation
        using var bufferWriter = Settings.Serializer.Write(item);
        await RedisDb.Database.StreamAddAsync(
                Key, Settings.ItemKey, bufferWriter.WrittenMemory,
                maxLength: Settings.MaxStreamLength,
                useApproximateMaxLength: true)
            .ConfigureAwait(false);
        await appendPub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    private async Task AppendEnd(
        Exception? error,
        RedisPub appendPub)
    {
        var finalStatus = Settings.EndedStatus;
        if (error != null)
            finalStatus = Settings.ErrorSerializer.Write(error);
        await RedisDb.Database.StreamAddAsync(
                Key, Settings.StatusKey, finalStatus,
                maxLength: Settings.MaxStreamLength,
                useApproximateMaxLength: true)
            .ConfigureAwait(false);
        await appendPub.Publish(RedisValue.EmptyString).ConfigureAwait(false);
    }

    public Task Remove()
        => RedisDb.Database.KeyDeleteAsync(Key, CommandFlags.FireAndForget);


    // Protected methods

    private RedisPub GetAppendPub()
        => RedisDb.GetPub(Key + Settings.AppendPubKeySuffix);

    private RedisTaskSub GetAppendSub()
        => RedisDb.GetTaskSub(
            (Key + Settings.AppendPubKeySuffix, RedisChannel.PatternMode.Literal),
            Settings.AppendSubscribeTimeout);
}
