using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisHashTest : RedisTestBase
{
    public RedisHashTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return; // No Redis on build agent for now

        var hash = GetRedisDb().GetHash("hash");
        await hash.Clear();
        (await hash.GetAll()).Length.Should().Be(0);

        (await hash.Increment("a")).Should().Be(1);
        (await hash.Increment("a")).Should().Be(2);
        (await hash.Set("a", "10")).Should().Be(false);
        (await hash.Increment("a")).Should().Be(11);
        (await hash.GetAll()).Length.Should().Be(1);

        (await hash.Increment("b")).Should().Be(1);
        (await hash.Increment("a")).Should().Be(12);
        (await hash.GetAll()).Length.Should().Be(2);

        (await hash.Remove("a")).Should().BeTrue();
        (await hash.GetAll()).Length.Should().Be(1);

        await hash.Clear();
        (await hash.GetAll()).Length.Should().Be(0);
    }
}
