using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisTaskSub : RedisSubBase
{
    private TaskCompletionSource<RedisValue> _nextMessageSource = null!;

    public RedisTaskSub(RedisDb redisDb, RedisSubKey key,
        TimeSpan? subscribeTimeout = null)
        : base(redisDb, key, subscribeTimeout, subscribe: false)
    {
        Reset();
        _ = Subscribe();
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        lock (Lock)
            _nextMessageSource.TrySetCanceled();
    }

    public Task<RedisValue> NextMessage(Task<RedisValue>? unprocessedMessageTask = null)
    {
        // We assume here that unprocessedMessageTask was unresolved
        // last time it was awaited, so if it's the case,
        // we should return it here, because in fact the message
        // we were waiting for earlier is here now.
        if (unprocessedMessageTask != null)
            return unprocessedMessageTask;
        var nextMessageSource = _nextMessageSource;
        if (!nextMessageSource.Task.IsCompleted)
            return nextMessageSource.Task;
        lock (Lock) {
            nextMessageSource = _nextMessageSource;
            if (!nextMessageSource.Task.IsCompleted)
                return nextMessageSource.Task;
            Reset();
            return _nextMessageSource.Task;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        lock (Lock)
            _nextMessageSource.TrySetResult(redisValue);
    }

    private void Reset()
    {
        _nextMessageSource = TaskCompletionSourceExt.New<RedisValue>();
        if (WhenDisposed != null)
            _nextMessageSource.TrySetCanceled();
    }
}

public sealed class RedisTaskSub<T> : RedisSubBase
{
    private TaskCompletionSource<T> _nextMessageSource = null!;

    public IByteSerializer<T> Serializer { get; }

    public RedisTaskSub(RedisDb redisDb, RedisSubKey key,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
        : base(redisDb, key, subscribeTimeout, subscribe: false)
    {
        Serializer = serializer ?? ByteSerializer<T>.Default;
        Reset();
        _ = Subscribe();
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        lock (Lock)
            _nextMessageSource.TrySetCanceled();
    }

    public Task<T> NextMessage(Task<T>? unresolvedMessageTask = null)
    {
        // We assume here that unresolvedMessageTask was unresolved
        // last time it was awaited, so if it's the case,
        // we should return it here, because in fact the message
        // we were waiting for earlier is here now.
        if (unresolvedMessageTask != null)
            return unresolvedMessageTask;
        var nextMessageSource = _nextMessageSource;
        if (!nextMessageSource.Task.IsCompleted)
            return nextMessageSource.Task;
        lock (Lock) {
            nextMessageSource = _nextMessageSource;
            if (!nextMessageSource.Task.IsCompleted)
                return nextMessageSource.Task;
            Reset();
            return _nextMessageSource.Task;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        try {
            var value = Serializer.Read(redisValue);
            lock (Lock)
                _nextMessageSource.TrySetResult(value);
        }
        catch (Exception e) {
            lock (Lock)
                _nextMessageSource.TrySetException(e);
        }
    }

    private void Reset()
    {
        _nextMessageSource = TaskCompletionSourceExt.New<T>();
        if (WhenDisposed != null)
            _nextMessageSource.TrySetCanceled();
    }
}
