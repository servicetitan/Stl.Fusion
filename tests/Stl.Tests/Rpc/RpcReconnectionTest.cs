using Stl.OS;
using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcReconnectionTest(ITestOutputHelper @out) : RpcLocalTestBase(@out)
{
    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var commander = services.AddCommander();
        commander.AddService<TestRpcService>();

        var rpc = services.AddRpc();
        rpc.AddServer<ITestRpcService, TestRpcService>();
        rpc.AddClient<ITestRpcService, ITestRpcServiceClient>();
    }

    [Fact]
    public async Task BasicTest()
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<ITestRpcServiceClient>();
        await client.Add(1, 1); // Warm-up

        var delay = TimeSpan.FromMilliseconds(100);
        var task = client.Delay(delay);
        connection.Disconnect();
        await Delay(0.05);
        await connection.Connect();
        (await task).Should().Be(delay);

        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task BasicStreamTest()
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var stream = await client.StreamInt32(100, -1, new RandomTimeSpan(0.02, 1));
        var countTask = stream.CountAsync();

        var disruptorCts = new CancellationTokenSource();
        var disruptorTask = ConnectionDisruptor(connection, disruptorCts.Token);
        try {
            (await countTask).Should().Be(100);
        }
        finally {
            disruptorCts.CancelAndDisposeSilently();
            await disruptorTask;
        }
    }

    [Fact(Timeout = 30_000)]
    public async Task ConcurrentTest()
    {
        var workerCount = HardwareInfo.ProcessorCount / 2;
        var testDuration = TimeSpan.FromSeconds(10);
        if (TestRunnerInfo.IsBuildAgent()) {
            workerCount = 1;
            testDuration = TimeSpan.FromSeconds(1);
        }

        var endAt = CpuTimestamp.Now + testDuration;
        var tasks = Enumerable.Range(0, workerCount)
            .Select(i => Task.Run(() => Worker(i, endAt)))
            .ToArray();
        var callCount = (await Task.WhenAll(tasks)).Sum();
        Out.WriteLine($"Call count: {callCount}");
        callCount.Should().BeGreaterThan(0);
    }

    private async Task<long> Worker(int workerIndex, CpuTimestamp endAt)
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var client = services.GetRequiredService<ITestRpcServiceClient>();
        await client.Add(1, 1); // Warm-up

        var timeout = TimeSpan.FromSeconds(5);
        var rnd = new Random();
        var callCount = 0L;

        var disruptorCts = new CancellationTokenSource();
        var disruptorTask = ConnectionDisruptor(connection, disruptorCts.Token);
        try {
            while (CpuTimestamp.Now < endAt) {
                var delay = TimeSpan.FromMilliseconds(rnd.Next(5, 120));
                var delayTask = client.Delay(delay).WaitAsync(timeout);
                (await delayTask).Should().Be(delay);
                callCount++;
            }
        }
        finally {
            disruptorCts.CancelAndDisposeSilently();
            await disruptorTask;
        }

        await AssertNoCalls(connection.ClientPeer);
        await AssertNoCalls(connection.ServerPeer);
        return callCount;
    }

    [Fact(Timeout = 30_000)]
    public async Task ConcurrentStreamTest()
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var workerCount = 2;
        if (TestRunnerInfo.IsBuildAgent())
            workerCount = 1;
        var tasks = Enumerable.Range(0, workerCount)
            .Select(async workerIndex => {
                var totalCount = 300;
                var stream = await client.StreamInt32(totalCount, -1, new RandomTimeSpan(0.02, 1));
                var count = 0;
                await foreach (var item in stream) {
                    count++;
                    if (item % 10 == 0)
                        Out.WriteLine($"{workerIndex}: {item}");
                }
                count.Should().Be(totalCount);
            })
            .ToArray();

        var disruptorCts = new CancellationTokenSource();
        _ = ConnectionDisruptor(connection, disruptorCts.Token);
        try {
            await Task.WhenAll(tasks);
        }
        finally {
            disruptorCts.CancelAndDisposeSilently();
        }
    }

// Private methods

    private async Task ConnectionDisruptor(RpcTestConnection connection, CancellationToken cancellationToken)
    {
        try {
            var rnd1 = new Random();
            while (true) {
                await Task.Delay(rnd1.Next(100, 150), cancellationToken);
                connection.Disconnect();
                await Task.Delay(rnd1.Next(20), cancellationToken);
                await connection.Connect(cancellationToken);
            }
        }
        catch {
            // Intended
        }
        await connection.Connect(CancellationToken.None);
        await Delay(0.2); // Just in case
    }
}
