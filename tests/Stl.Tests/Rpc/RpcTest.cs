using Stl.Rpc;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcTest : RpcTestBase
{
    public RpcTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServices();
        var peer = services.RpcHub().GetPeer(default);
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
    public async Task DelayTest()
    {
        var services = CreateServices();
        var peer = services.RpcHub().GetPeer(default);
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
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10_000)]
    [InlineData(30_000)]
    [InlineData(100_000)]
    public async Task PerformanceTest(int iterationCount)
    {
        var services = CreateServices();
        var peer = services.RpcHub().GetPeer(default);
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.Div(1, 1);

        var startedAt = CpuTimestamp.Now;
        for (var i = iterationCount; i > 0; i--)
            if (i != await client.Div(i, 1).ConfigureAwait(false))
                Assert.Fail("Wrong result.");
        var elapsed = startedAt.Elapsed;

        Out.WriteLine($"{iterationCount}: {iterationCount / elapsed.TotalSeconds:F} ops/s");
        peer.Calls.Outbound.Count.Should().Be(0);
        peer.Calls.Inbound.Count.Should().Be(0);
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<SimpleRpcService>();

        var rpc = services.AddRpc();
        rpc.AddService<ISimpleRpcService, SimpleRpcService>()
            .AddClient<ISimpleRpcServiceClient>();
    }
}
