using StackExchange.Redis;

namespace Stl.Redis;

public abstract class RedisSubBase : IAsyncDisposable, IHasDisposeStarted
{
    public static TimeSpan DefaultSubscribeTimeout { get; set; } = TimeSpan.FromSeconds(5);

    private readonly Action<RedisChannel, RedisValue> _onMessage;
    private readonly CancellationTokenSource _subscribeTimeoutCts;
    protected object Lock => _onMessage;

    public RedisDb RedisDb { get; }
    public string Key { get; }
    public RedisChannel.PatternMode PatternMode { get; }
    public string FullKey { get; }
    public ISubscriber Subscriber { get; }
    public RedisChannel RedisChannel { get; }
    public Task WhenSubscribed { get; private set; } = null!;
    public bool IsDisposeStarted { get; private set; }

    protected RedisSubBase(RedisDb redisDb, RedisSubKey key,
        TimeSpan? subscribeTimeout = null,
        bool subscribe = true)
    {
        RedisDb = redisDb;
        Key = key.Key;
        PatternMode = key.PatternMode;
        FullKey = RedisDb.FullKey(Key);
        Subscriber = RedisDb.Redis.GetSubscriber();
        RedisChannel = new RedisChannel(FullKey, PatternMode);
        _onMessage = OnMessage;
        _subscribeTimeoutCts = new CancellationTokenSource(subscribeTimeout ?? DefaultSubscribeTimeout);
        if (subscribe)
            Subscribe();
    }

    protected abstract void OnMessage(RedisChannel redisChannel, RedisValue redisValue);

    protected void Subscribe()
    {
        WhenSubscribed = Task.Run(async () => {
            var cancellationToken = _subscribeTimeoutCts.Token;
            try {
                await Subscriber
                    .SubscribeAsync(RedisChannel, _onMessage)
                    .WithFakeCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                if (IsDisposeStarted)
                    throw;
                if (cancellationToken.IsCancellationRequested)
                    throw new TimeoutException();
                throw;
            }
        }, CancellationToken.None);
    }

    // DisposeAsync

    public async ValueTask DisposeAsync()
    {
        if (IsDisposeStarted)
            return;
        lock (Lock) {
            if (IsDisposeStarted)
                return;
            IsDisposeStarted = true;
        }
        _subscribeTimeoutCts.CancelAndDisposeSilently();
        try {
            if (!WhenSubscribed.IsCompleted)
                await WhenSubscribed.ConfigureAwait(false);
        }
        catch {
            // Intended
        }
        try {
            await Subscriber
                .UnsubscribeAsync(RedisChannel, _onMessage, CommandFlags.FireAndForget)
                .ConfigureAwait(false);
        }
        catch {
            // Intended
        }
        await DisposeAsyncInternal().ConfigureAwait(false);
    }

    protected virtual ValueTask DisposeAsyncInternal()
        => ValueTaskExt.CompletedTask;
}
