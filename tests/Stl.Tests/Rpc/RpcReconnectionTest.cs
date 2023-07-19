using Stl.OS;
using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcReconnectionTest : RpcLocalTestBase
{
    public RpcReconnectionTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var commander = services.AddCommander();
        commander.AddCommandService<TestRpcService>();

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

    [Fact(Timeout = 30_000)]
    public async Task ReconnectionTest()
    {
        var workerCount = HardwareInfo.ProcessorCount / 2;
        var testDuration = TimeSpan.FromSeconds(10);
        if (TestRunnerInfo.IsGitHubAction()) {
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
        var disruptorCts = new CancellationTokenSource();
        var disruptorTask = Task.Run(() => ConnectionDisruptor(disruptorCts.Token));
        try {
            var rnd = new Random();
            var callCount = 0L;
            while (CpuTimestamp.Now < endAt) {
                var delay = TimeSpan.FromMilliseconds(rnd.Next(5, 120));
                var delayTask = client.Delay(delay).WaitAsync(timeout);
                (await delayTask).Should().Be(delay);
                callCount++;
            }

            disruptorCts.CancelAndDisposeSilently();
            await disruptorTask.WaitAsync(timeout).SuppressCancellationAwait();
            await connection.Connect().WaitAsync(timeout);
            await Delay(0.2); // Just in case

            await AssertNoCalls(connection.ClientPeer);
            await AssertNoCalls(connection.ServerPeer);
            return callCount;
        }
        finally {
            disruptorCts.CancelAndDisposeSilently();
        }

        async Task ConnectionDisruptor(CancellationToken cancellationToken)
        {
            var rnd1 = new Random();
            while (CpuTimestamp.Now < endAt) {
                await Task.Delay(rnd1.Next(10, 40), cancellationToken);
                connection.Disconnect();
                await Task.Delay(rnd1.Next(10, 20), cancellationToken);
                await connection.Connect(cancellationToken).WaitAsync(timeout, cancellationToken);
            }
        }
    }
}
