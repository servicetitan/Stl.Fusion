using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class FusionRpcReconnectionTest : SimpleFusionTestBase
{
    public FusionRpcReconnectionTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        var fusion = services.AddFusion();
        fusion.AddService<ReconnectTester>();
        fusion.AddClient<IReconnectTester>(nameof(ReconnectTester));
        fusion.Rpc.Service<IReconnectTester>().HasServer<ReconnectTester>();
    }

    [Fact]
    public async Task Case1Test()
    {
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<IReconnectTester>();

        var (delay, invDelay) = (300, 300);
        var task = client.Delay(delay, invDelay);

        await Delay(0.05);
        await connection.Reconnect();
        // Call is still running on server, so recovery will pull its result

        (await task.WaitAsync(TimeSpan.FromSeconds(1))).Should().Be((delay, invDelay));
        var computed = await Computed
            .Capture(() => client.Delay(delay, invDelay))
            .AsTask().WaitAsync(TimeSpan.FromSeconds(0.1)); // Should be instant
        computed.IsConsistent().Should().BeTrue();

        var startedAt = CpuTimestamp.Now;
        await computed.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1));
        var elapsed = CpuTimestamp.Now - startedAt;
        if (!TestRunnerInfo.IsBuildAgent())
            elapsed.TotalSeconds.Should().BeGreaterThan(0.1);

        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task Case2Test()
    {
        var waitMultiplier = TestRunnerInfo.IsBuildAgent() ? 10 : 1;
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<IReconnectTester>();

        var (delay, invDelay) = (300, 300);
        var task = client.Delay(delay, invDelay);
        (await task.WaitAsync(TimeSpan.FromSeconds(1 * waitMultiplier))).Should().Be((delay, invDelay));
        var computed = await Computed
            .Capture(() => client.Delay(delay, invDelay))
            .AsTask().WaitAsync(TimeSpan.FromSeconds(0.1)); // Should be instant
        computed.IsConsistent().Should().BeTrue();

        await connection.Reconnect();
        // Recovery is expected to trigger result update and/or invalidation

        var startedAt = CpuTimestamp.Now;
        await computed.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1 * waitMultiplier));
        var elapsed = CpuTimestamp.Now - startedAt;
        if (!TestRunnerInfo.IsBuildAgent())
            elapsed.TotalSeconds.Should().BeGreaterThan(0.1);

        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task Case3Test()
    {
        var waitMultiplier = TestRunnerInfo.IsBuildAgent() ? 10 : 1;
        await using var services = CreateServices();
        var connection = services.GetRequiredService<RpcTestClient>().Single();
        var clientPeer = connection.ClientPeer;
        var client = services.GetRequiredService<IReconnectTester>();

        var (delay, invDelay) = (200, 200);
        var task = client.Delay(delay, invDelay);

        await connection.Reconnect(TimeSpan.FromSeconds(1));
        // Inbound call is gone.
        // Recovery is expected to simply repeat the call

        (await task.WaitAsync(TimeSpan.FromSeconds(1 * waitMultiplier))).Should().Be((delay, invDelay));
        var computed = await Computed
            .Capture(() => client.Delay(delay, invDelay))
            .AsTask().WaitAsync(TimeSpan.FromSeconds(0.1)); // Should be instant

        var startedAt = CpuTimestamp.Now;
        await computed.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(1 * waitMultiplier));
        var elapsed = CpuTimestamp.Now - startedAt;
        if (!TestRunnerInfo.IsBuildAgent())
            elapsed.TotalSeconds.Should().BeGreaterThan(0.1);

        await AssertNoCalls(clientPeer);
    }

    [Fact(Timeout = 30_000)]
    public async Task ReconnectionTest()
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
        var client = services.GetRequiredService<IReconnectTester>();
        await client.Delay(1, 1); // Warm-up

        var timeout = TimeSpan.FromSeconds(5);
        var disruptorCts = new CancellationTokenSource();
        var disruptorTask = Task.Run(() => ConnectionDisruptor(disruptorCts.Token));
        try {
            var rnd = new Random();
            var callCount = 0L;
            while (CpuTimestamp.Now < endAt) {
                var delay = rnd.Next(10, 100);
                var invDelay = rnd.Next(10, 100);
                var result = await client.Delay(delay, invDelay).WaitAsync(timeout);
                result.Should().Be((delay, invDelay));
                callCount++;
            }

            disruptorCts.CancelAndDisposeSilently();
            await disruptorTask.WaitAsync(timeout).SuppressCancellationAwait();
            await connection.Connect().WaitAsync(timeout);
            await Delay(0.2); // Enough for invalidations to come through

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
                await Task.Delay(rnd1.Next(10, 80), cancellationToken);
                connection.Disconnect();
                await Task.Delay(rnd1.Next(10, 40), cancellationToken);
                await connection.Connect(cancellationToken).WaitAsync(timeout, cancellationToken);
            }
        }
    }
}
