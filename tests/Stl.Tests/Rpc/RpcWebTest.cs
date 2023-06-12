using Stl.OS;
using Stl.Rpc;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcWebTest : RpbWebTestBase
{
    public RpcWebTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetPeer(ClientPeerId);
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(10, 2)).Should().Be(5);
        (await client.Div(null, 2)).Should().Be(null);
        await Assert.ThrowsAsync<DivideByZeroException>(
            () => client.Div(1, 0));

        peer.Calls.Outbound.Count.Should().Be(0);
        peer.Calls.Inbound.Count.Should().Be(0);
    }

    [Fact]
    public async Task CommandTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var commander = services.Commander();
        await commander.Call(new ISimpleRpcService.DummyCommand("ok"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => commander.Call(new ISimpleRpcService.DummyCommand("error")));

        var peer = services.RpcHub().GetPeer(ClientPeerId);
        peer.Calls.Outbound.Count.Should().Be(0);
        peer.Calls.Inbound.Count.Should().Be(0);
    }

    [Fact]
    public async Task DelayTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetPeer(ClientPeerId);
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        var startedAt = CpuTimestamp.Now;
        await client.Delay(TimeSpan.FromMilliseconds(200));
        startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(100, 500);
        peer.Calls.Outbound.Count.Should().Be(0);
        peer.Calls.Inbound.Count.Should().Be(0);

        {
            using var cts = new CancellationTokenSource(1);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(0, 500);
            peer.Calls.Outbound.Count.Should().Be(0);
            peer.Calls.Inbound.Count.Should().Be(0);
        }

        {
            using var cts = new CancellationTokenSource(500);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(300, 1000);
            peer.Calls.Outbound.Count.Should().Be(0);
            peer.Calls.Inbound.Count.Should().Be(0);
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(50_000)]
    public async Task PerformanceTest(int iterationCount)
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetPeer(ClientPeerId);
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();

        var threadCount = Math.Max(1, HardwareInfo.ProcessorCount);
        var tasks = new Task[threadCount];
        await Run(10); // Warmup
        var elapsed = await Run(iterationCount);

        var totalIterationCount = threadCount * iterationCount;
        Out.WriteLine($"{iterationCount}: {totalIterationCount / elapsed.TotalSeconds:F} ops/s using {threadCount} threads");
        peer.Calls.Outbound.Count.Should().Be(0);
        peer.Calls.Inbound.Count.Should().Be(0);

        async Task<TimeSpan> Run(int count)
        {
            var startedAt = CpuTimestamp.Now;
            for (var threadIndex = 0; threadIndex < threadCount; threadIndex++) {
                tasks[threadIndex] = Task.Run(async () => {
                    for (var i = count; i > 0; i--)
                        if (i != await client.Div(i, 1).ConfigureAwait(false))
                            Assert.Fail("Wrong result.");
                }, CancellationToken.None);
            }

            await Task.WhenAll(tasks);
            return elapsed = startedAt.Elapsed;
        }
    }

    protected override void ConfigureServices(IServiceCollection services, bool isClient = false)
    {
        base.ConfigureServices(services, isClient);
        var rpc = services.AddRpc();
        var commander = services.AddCommander();
        if (isClient) {
            commander.AddCommandService<ISimpleRpcServiceClient>();
            rpc.Service<ISimpleRpcService>().HasClient<ISimpleRpcServiceClient>();
        }
        else {
            commander.AddCommandService<SimpleRpcService>();
            rpc.Service<ISimpleRpcService>().HasServer<SimpleRpcService>();
        }
    }
}
