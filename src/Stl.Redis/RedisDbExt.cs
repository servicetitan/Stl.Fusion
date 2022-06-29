using StackExchange.Redis;

namespace Stl.Redis;

public static class RedisDbExt
{
    public static RedisPub GetPub(this RedisDb redisDb, string key)
        => new(redisDb, key);
    public static RedisPub<T> GetPub<T>(this RedisDb redisDb, string key,
        IByteSerializer<T>? serializer = null)
        => new(redisDb, key, serializer);

    public static RedisActionSub GetActionSub(this RedisDb redisDb, RedisSubKey key,
        Action<RedisChannel, RedisValue> messageHandler,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, messageHandler, subscribeTimeout);
    public static RedisActionSub<T> GetActionSub<T>(this RedisDb redisDb, RedisSubKey key,
        Action<RedisChannel, T> messageHandler,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, messageHandler, serializer, subscribeTimeout);

    public static RedisTaskSub GetTaskSub(this RedisDb redisDb, RedisSubKey key,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, subscribeTimeout);
    public static RedisTaskSub<T> GetTaskSub<T>(this RedisDb redisDb, RedisSubKey key,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, serializer, subscribeTimeout);

    public static RedisChannelSub GetChannelSub(this RedisDb redisDb, RedisSubKey key,
        Channel<RedisValue>? channel = null,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, channel, subscribeTimeout);
    public static RedisChannelSub<T> GetChannelSub<T>(this RedisDb redisDb, RedisSubKey key,
        Channel<T>? channel = null,
        IByteSerializer<T>? serializer = null,
        TimeSpan? subscribeTimeout = null)
        => new(redisDb, key, channel, serializer, subscribeTimeout);

    public static RedisQueue<T> GetQueue<T>(this RedisDb redisDb, string key, RedisQueue<T>.Options? settings = null)
        => new(redisDb, key, settings);

    public static RedisHash GetHash(this RedisDb redisDb, string hashKey)
        => new(redisDb, hashKey);

    public static RedisSequenceSet GetSequenceSet(
        this RedisDb redisDb, string hashKey)
        => new(redisDb.GetHash(hashKey));
    public static RedisSequenceSet<TScope> GetSequenceSet<TScope>(
        this RedisDb redisDb, string hashKey)
        => new(redisDb.GetHash(hashKey));

    public static RedisSequenceSet GetSequenceSet(
        this RedisDb redisDb, string hashKey, long resetRange)
        => new(redisDb.GetHash(hashKey)) { ResetRange = resetRange };
    public static RedisSequenceSet<TScope> GetSequenceSet<TScope>(
        this RedisDb redisDb, string hashKey, long resetRange)
        => new(redisDb.GetHash(hashKey)) { ResetRange = resetRange };

    public static RedisStreamer<T> GetStreamer<T>(this RedisDb redisDb, string key, RedisStreamer<T>.Options? settings = null)
        => new(redisDb, key, settings);
}
