using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisTaskSub : RedisSubBase
{
    private Task<RedisValue> _nextMessageTask = null!;

    public RedisTaskSub(RedisDb redisDb, string key)
        : base(redisDb, key)
        => Reset();

    protected override ValueTask DisposeAsyncInternal()
    {
        lock (Lock)
            TaskSource.For(_nextMessageTask).TrySetCanceled();
        return ValueTaskExt.CompletedTask;
    }

    public Task<RedisValue> NextMessage(Task<RedisValue>? unresolvedMessageTask = null)
    {
        // We assume here that unresolvedMessageTask was unresolved
        // last time it was awaited, so if it's the case,
        // we should return it here, because in fact the message
        // we were waiting for earlier is here now.
        if (unresolvedMessageTask != null)
            return unresolvedMessageTask;
        var nextMessageTask = _nextMessageTask;
        if (!nextMessageTask.IsCompleted)
            return nextMessageTask;
        lock (Lock) {
            nextMessageTask = _nextMessageTask;
            if (!nextMessageTask.IsCompleted)
                return nextMessageTask;
            Reset();
            return _nextMessageTask;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        lock (Lock)
            TaskSource.For(_nextMessageTask).TrySetResult(redisValue);
    }

    private void Reset()
    {
        _nextMessageTask = TaskSource.New<RedisValue>(true).Task;
        if (IsDisposeStarted)
            TaskSource.For(_nextMessageTask).TrySetCanceled();
    }
}

public sealed class RedisTaskSub<T> : RedisSubBase
{
    private Task<T> _nextMessageTask = null!;

    public IByteSerializer<T> Serializer { get; }

    public RedisTaskSub(RedisDb redisDb, string key,
        IByteSerializer<T>? serializer = null)
        : base(redisDb, key)
    {
        Serializer = serializer ?? ByteSerializer<T>.Default;
        Reset();
    }

    protected override ValueTask DisposeAsyncInternal()
    {
        lock (Lock)
            TaskSource.For(_nextMessageTask).TrySetCanceled();
        return ValueTaskExt.CompletedTask;
    }

    public Task<T> NextMessage(Task<T>? unresolvedMessageTask = null)
    {
        // We assume here that unresolvedMessageTask was unresolved
        // last time it was awaited, so if it's the case,
        // we should return it here, because in fact the message
        // we were waiting for earlier is here now.
        if (unresolvedMessageTask != null)
            return unresolvedMessageTask;
        var nextMessageTask = _nextMessageTask;
        if (!nextMessageTask.IsCompleted)
            return nextMessageTask;
        lock (Lock) {
            nextMessageTask = _nextMessageTask;
            if (!nextMessageTask.IsCompleted)
                return nextMessageTask;
            Reset();
            return _nextMessageTask;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        try {
            var value = Serializer.Read(redisValue);
            lock (Lock)
                TaskSource.For(_nextMessageTask).TrySetResult(value);
        }
        catch (Exception e) {
            lock (Lock)
                TaskSource.For(_nextMessageTask).TrySetException(e);
        }
    }

    private void Reset()
    {
        _nextMessageTask = TaskSource.New<T>(true).Task;
        if (IsDisposeStarted)
            TaskSource.For(_nextMessageTask).TrySetCanceled();
    }
}

