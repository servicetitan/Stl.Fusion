using StackExchange.Redis;
using Stl.Internal;

namespace Stl.Redis;

public abstract class RedisSubBase : IAsyncDisposable
{
    private static readonly Task AlreadyDisposedTask = Task.FromException(Errors.AlreadyDisposedOrDisposing());
    private readonly Action<RedisChannel, RedisValue> _onMessage;
    protected object Lock => _onMessage;

    public RedisDb RedisDb { get; }
    public string Key { get; }
    public string FullKey { get; }
    public ISubscriber Subscriber { get; }
    public RedisChannel RedisChannel { get; }
    public Task WhenSubscribed { get; private set; }

    protected RedisSubBase(RedisDb redisDb, string key,
        RedisChannel.PatternMode patternMode = RedisChannel.PatternMode.Auto)
    {
        RedisDb = redisDb;
        Key = key;
        FullKey = RedisDb.FullKey(Key);
        Subscriber = RedisDb.Redis.GetSubscriber();
        RedisChannel = new RedisChannel(FullKey, patternMode);
        _onMessage = OnMessage;
        WhenSubscribed = Subscriber.SubscribeAsync(RedisChannel, _onMessage);
    }

    protected abstract void OnMessage(RedisChannel redisChannel, RedisValue redisValue);

    public async ValueTask DisposeAsync()
    {
        var whenSubscribed = WhenSubscribed;
        WhenSubscribed = AlreadyDisposedTask;
        if (!whenSubscribed.IsCompletedSuccessfully())
            return;

        try {
            if (!whenSubscribed.IsCompleted)
                await whenSubscribed.ConfigureAwait(false);
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
    }
}
