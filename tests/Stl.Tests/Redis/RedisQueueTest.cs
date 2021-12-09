using Stl.Redis;

namespace Stl.Tests.Redis;

public class RedisQueueTest : RedisTestBase
{
    public RedisQueueTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return; // No Redis on build agent for now

        var sw = new Stopwatch();
        sw.Start();
        var queue = GetRedisDb().GetQueue<string>("q");
        await queue.Remove();
        await using var _ = queue.ConfigureAwait(false);

        Out.WriteLine($"{sw.ElapsedMilliseconds}: <- 1");
        await queue.Enqueue("1");
        Out.WriteLine($"{sw.ElapsedMilliseconds}: -> {await queue.Dequeue()}");

        Out.WriteLine($"{sw.ElapsedMilliseconds}: <- 2");
        await queue.Enqueue("2");
        Out.WriteLine($"{sw.ElapsedMilliseconds}: -> {await queue.Dequeue()}");
    }
}
