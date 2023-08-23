using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisHashTest(ITestOutputHelper @out) : RedisTestBase(@out)
{
    [SkipOnGitHubFact]
    public async Task BasicTest()
    {
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
