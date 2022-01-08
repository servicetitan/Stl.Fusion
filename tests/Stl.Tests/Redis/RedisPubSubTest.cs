using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisPubSubTest : RedisTestBase
{
    public RedisPubSubTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return; // No Redis on build agent for now

        var sw = new Stopwatch();
        sw.Start();
        var pub1 = GetRedisDb().GetPub<string>("pub-1");
        var pub2 = GetRedisDb().GetPub<string>("pub-2");
        var sub = GetRedisDb().GetTaskSub<string>("pub-*");
        await using var _ = sub.ConfigureAwait(false);

        await sub.WhenSubscribed.ConfigureAwait(false);
        Out.WriteLine($"{sw.ElapsedMilliseconds}: <- 1");
        var messageTask = sub.NextMessage();
        await pub1.Publish("1");
        Out.WriteLine($"{sw.ElapsedMilliseconds}: -> {await messageTask}");

        Out.WriteLine($"{sw.ElapsedMilliseconds}: <- 2");
        messageTask = sub.NextMessage();
        await pub2.Publish("2");
        Out.WriteLine($"{sw.ElapsedMilliseconds}: -> {await messageTask}");
    }
}
