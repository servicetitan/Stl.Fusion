using Stl.Diagnostics;
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
        commander.AddCommandService<SimpleRpcService>();

        var rpc = services.AddRpc();
        rpc.AddServer<ISimpleRpcService, SimpleRpcService>();
        rpc.AddClient<ISimpleRpcService, ISimpleRpcServiceClient>();
    }

    [Fact]
    public async Task BasicTest()
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.Add(1, 1); // Warm-up

        var delay = TimeSpan.FromMilliseconds(100);
        var task = client.Delay(delay);
        connection.Disconnect();
        await Delay(0.05);
        await connection.Connect();
        (await task).Should().Be(delay);

        await AssertNoCalls(clientPeer);
    }

    [Theory]
    [InlineData(5)]
    public async Task ReconnectionTest(double testDuration)
    {
        var workerCount = 1; // HardwareInfo.ProcessorCount * 1 / 8;
        var endAt = CpuTimestamp.Now + TimeSpan.FromSeconds(testDuration);
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
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.Add(1, 1); // Warm-up

        var disruptorCts = new CancellationTokenSource();
        var disruptorTask = Task.Run(() => ConnectionDisruptor(disruptorCts.Token));

        var rnd = new Random();
        var callCount = 0L;
        while (CpuTimestamp.Now < endAt) {
            var delay = TimeSpan.FromMilliseconds(rnd.Next(1, 20));
            (await client.Delay(delay).WaitAsync(TimeSpan.FromSeconds(1))).Should().Be(delay);
            callCount++;
        }

        disruptorCts.CancelAndDisposeSilently();
        await disruptorTask.SilentAwait();

        await AssertNoCalls(clientPeer);
        return callCount;

        async Task ConnectionDisruptor(CancellationToken cancellationToken)
        {
            var rnd1 = new Random();
            while (CpuTimestamp.Now < endAt) {
                await Task.Delay(rnd1.Next(1, 40), cancellationToken);
                connection.Disconnect();
                await Task.Delay(rnd1.Next(1, 20), cancellationToken);
                await connection.Connect(cancellationToken);
            }
        }
    }
}
