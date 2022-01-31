using Cysharp.Text;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Stl.Redis;

public class RedisDb
{
    public IConnectionMultiplexer Redis { get; }
    public string KeyPrefix { get; }
    public IDatabase Database { get; }

    public RedisDb(IConnectionMultiplexer redis, string? keyPrefix = null)
    {
        Redis = redis;
        KeyPrefix = keyPrefix ?? "";
        Database = Redis.GetDatabase();
        if (!KeyPrefix.IsNullOrEmpty())
            Database = Database.WithKeyPrefix(KeyPrefix);
    }

    public override string ToString()
        => $"{GetType().Name}(KeyPrefix = {KeyPrefix})";

    public string FullKey(string keySuffix)
        => KeyPrefix.IsNullOrEmpty()
            ? keySuffix
            : keySuffix.IsNullOrEmpty()
                ? KeyPrefix
                : ZString.Concat(KeyPrefix, '.', keySuffix);

    public RedisDb WithKeyPrefix(string keyPrefix)
        => keyPrefix.IsNullOrEmpty()
            ? this
            : new RedisDb(Redis, FullKey(keyPrefix));
}

public class RedisDb<TContext> : RedisDb
{
    public RedisDb(IConnectionMultiplexer redis, string? keyPrefix = null)
        : base(redis, keyPrefix)
    { }
}
