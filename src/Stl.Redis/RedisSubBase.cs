using StackExchange.Redis;
using Stl.Internal;

namespace Stl.Redis;

public abstract class RedisSubBase : IAsyncDisposable, IHasDisposeStarted
{
    public static TimeSpan DefaultSubscribeTimeout { get; set; } = TimeSpan.FromSeconds(5);

    private static readonly Task AlreadyDisposedTask = Task.FromException(Errors.AlreadyDisposedOrDisposing());
    private readonly Action<RedisChannel, RedisValue> _onMessage;
    private readonly CancellationTokenSource _subscribeTimeoutCts;
    protected object Lock => _onMessage;

    public RedisDb RedisDb { get; }
    public string Key { get; }
    public RedisChannel.PatternMode PatternMode { get; }
    public string FullKey { get; }
    public ISubscriber Subscriber { get; }
    public RedisChannel RedisChannel { get; }
    public Task WhenSubscribed { get; private set; }
    public bool IsDisposeStarted { get; private set; }

    protected RedisSubBase(RedisDb redisDb, RedisSubKey key, TimeSpan? subscribeTimeout = null)
    {
        RedisDb = redisDb;
        Key = key.Key;
        PatternMode = key.PatternMode;
        FullKey = RedisDb.FullKey(Key);
        Subscriber = RedisDb.Redis.GetSubscriber();
        RedisChannel = new RedisChannel(FullKey, PatternMode);
        _onMessage = OnMessage;
        _subscribeTimeoutCts = new CancellationTokenSource(subscribeTimeout ?? DefaultSubscribeTimeout);
        WhenSubscribed = Task.Run(async () => {
            var cancellationToken = _subscribeTimeoutCts.Token;
            try {
                await Subscriber
                    .SubscribeAsync(RedisChannel, _onMessage)
                    .WithFakeCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested)
                    throw new TimeoutException();
                throw;
            }
        });
    }

    protected abstract void OnMessage(RedisChannel redisChannel, RedisValue redisValue);

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
