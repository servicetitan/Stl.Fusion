using Stl.Mathematics;

namespace Stl.Redis;

public static class RedisDbExt
{
    public static RedisPubSub GetPubSub(this RedisDb redisDb, string key)
        => new(redisDb, key);
    public static RedisPubSub<T> GetPubSub<T>(this RedisDb redisDb, string key)
        => new(redisDb, key);

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
        this RedisDb redisDb, string hashKey, Range<long> resetRange)
        => new(redisDb.GetHash(hashKey)) { ResetRange = resetRange };
    public static RedisSequenceSet<TScope> GetSequenceSet<TScope>(
        this RedisDb redisDb, string hashKey, Range<long> resetRange)
        => new(redisDb.GetHash(hashKey)) { ResetRange = resetRange };
}
