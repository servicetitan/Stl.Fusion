using StackExchange.Redis;

namespace Stl.Redis;

public sealed class RedisTaskSub : RedisSubBase
{
    private Task<RedisValue> _lastMessageTask = null!;

    public RedisTaskSub(RedisDb redisDb, string key)
        : base(redisDb, key)
        => Reset();

    public Task<RedisValue> NextMessage()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var lastMessageTask = _lastMessageTask;
        if (!lastMessageTask.IsCompleted)
            return lastMessageTask;
        lock (Lock) {
            lastMessageTask = _lastMessageTask;
            if (!lastMessageTask.IsCompleted)
                return lastMessageTask;
            Reset();
            return _lastMessageTask;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        lock (Lock)
            TaskSource.For(_lastMessageTask).TrySetResult(redisValue);
    }

    private void Reset()
        => _lastMessageTask = TaskSource.New<RedisValue>(true).Task;
}

public sealed class RedisTaskSub<T> : RedisSubBase
{
    private Task<T> _lastMessageTask = null!;

    public IByteSerializer<T> Serializer { get; }

    public RedisTaskSub(RedisDb redisDb, string key,
        IByteSerializer<T>? serializer = null)
        : base(redisDb, key)
    {
        Serializer = serializer ?? ByteSerializer<T>.Default;
        Reset();
    }

    public Task<T> NextMessage()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        var lastMessageTask = _lastMessageTask;
        if (!lastMessageTask.IsCompleted)
            return lastMessageTask;
        lock (Lock) {
            lastMessageTask = _lastMessageTask;
            if (!lastMessageTask.IsCompleted)
                return lastMessageTask;
            Reset();
            return _lastMessageTask;
        }
    }

    protected override void OnMessage(RedisChannel redisChannel, RedisValue redisValue)
    {
        try {
            var value = Serializer.Read(redisValue);
            lock (Lock)
                TaskSource.For(_lastMessageTask).TrySetResult(value);
        }
        catch (Exception e) {
            lock (Lock)
                TaskSource.For(_lastMessageTask).TrySetException(e);
        }
    }

    private void Reset()
        => _lastMessageTask = TaskSource.New<T>(true).Task;
}

