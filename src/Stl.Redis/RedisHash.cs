using StackExchange.Redis;

namespace Stl.Redis;

public class RedisHash
{
    public RedisDb RedisDb { get; }
    public string HashKey { get; }

    public RedisHash(RedisDb redisDb, string hashKey)
    {
        RedisDb = redisDb;
        HashKey = hashKey;
    }

    public Task<RedisValue> Get(string key)
        => RedisDb.Database.HashGetAsync(HashKey, key);

    public Task<HashEntry[]> GetAll()
        => RedisDb.Database.HashGetAllAsync(HashKey);

    public Task<bool> Set(string key, RedisValue value)
        => RedisDb.Database.HashSetAsync(HashKey, key, value);

    public Task<bool> Remove(string key)
        => RedisDb.Database.HashDeleteAsync(HashKey, key);

    public Task<long> Increment(string key, long increment = 1)
        => RedisDb.Database.HashIncrementAsync(HashKey, key, increment);

    public Task<bool> Clear()
        => RedisDb.Database.KeyDeleteAsync(HashKey);
}
