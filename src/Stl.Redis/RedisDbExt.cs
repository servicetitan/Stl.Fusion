using StackExchange.Redis;
using Stl.Mathematics;

namespace Stl.Redis;

public static class RedisDbExt
{
    public static RedisPub GetPub(this RedisDb redisDb, string key)
        => new(redisDb, key);
    public static RedisPub<T> GetPub<T>(this RedisDb redisDb, string key,
        IByteSerializer<T>? serializer = null)
        => new(redisDb, key, serializer);

    public static RedisActionSub GetActionSub(this RedisDb redisDb, string key,
        Action<RedisChannel, RedisValue> messageHandler,
        RedisChannel.PatternMode patternMode = RedisChannel.PatternMode.Auto)
        => new(redisDb, key, messageHandler, patternMode);
    public static RedisActionSub<T> GetActionSub<T>(this RedisDb redisDb, string key,
        Action<RedisChannel, T> messageHandler,
        IByteSerializer<T>? serializer = null,
        RedisChannel.PatternMode patternMode = RedisChannel.PatternMode.Auto)
        => new(redisDb, key, messageHandler, serializer, patternMode);

    public static RedisTaskSub GetTaskSub(this RedisDb redisDb, string key)
        => new(redisDb, key);
    public static RedisTaskSub<T> GetTaskSub<T>(this RedisDb redisDb, string key,
        IByteSerializer<T>? serializer = null)
        => new(redisDb, key, serializer);

    public static RedisChannelSub GetChannelSub(this RedisDb redisDb, string key,
        Channel<RedisValue>? channel = null)
        => new(redisDb, key, channel);
    public static RedisChannelSub<T> GetChannelSub<T>(this RedisDb redisDb, string key,
        Channel<T>? channel = null,
        IByteSerializer<T>? serializer = null)
        => new(redisDb, key, channel, serializer);

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
