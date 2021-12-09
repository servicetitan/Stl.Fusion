using StackExchange.Redis;
using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisTestBase : TestBase
{
    public RedisTestBase(ITestOutputHelper @out) : base(@out) { }

    public virtual RedisDb GetRedisDb()
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        return new RedisDb(redis).WithKeyPrefix("stl.fusion.tests").WithKeyPrefix(GetType().Name);
    }
}
