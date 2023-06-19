using Stl.Rpc;
using Stl.Rpc.Testing;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcBasicTest : RpcLocalTestBase
{
    public RpcBasicTest(ITestOutputHelper @out) : base(@out) { }

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
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(10, 2)).Should().Be(5);
        (await client.Div(null, 2)).Should().Be(null);
        await Assert.ThrowsAsync<DivideByZeroException>(
            () => client.Div(1, 0));
        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task CommandTest()
    {
        await using var services = CreateServices();
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.OnDummyCommand(new SimpleRpcServiceDummyCommand("ok"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.OnDummyCommand(new SimpleRpcServiceDummyCommand("error")));
        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task DelayTest()
    {
        await using var services = CreateServices();
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.Add(1, 1); // Warm-up

        var startedAt = CpuTimestamp.Now;
        await client.Delay(TimeSpan.FromMilliseconds(200));
        startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(100, 500);
        await AssertNoCalls(clientPeer);

        {
            using var cts = new CancellationTokenSource(1);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(0, 500);
            await AssertNoCalls(clientPeer);
        }

        {
            using var cts = new CancellationTokenSource(500);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(300, 1000);
            await AssertNoCalls(clientPeer);
        }
    }

    [Fact]
    public async Task PolymorphTest()
    {
        await using var services = CreateServices();
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        var a1 = new Tuple<int>(1);
        var r1 = await client.Polymorph(a1);
        r1.Should().Be(a1);
        r1.Should().NotBeSameAs(a1);
        await AssertNoCalls(clientPeer);
    }

    [Fact]
    public async Task CancellationTest()
    {
        await using var services = CreateServices();
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();

        var cts = new CancellationTokenSource(100);
        var result = await client.Delay(TimeSpan.FromMilliseconds(300), cts.Token).ResultAwait();
        result.Error.Should().BeAssignableTo<OperationCanceledException>();
        var cancellationCount = await client.GetCancellationCount();
        cancellationCount.Should().Be(1);
        await AssertNoCalls(clientPeer);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10_000)]
    [InlineData(50_000)]
    [InlineData(200_000)]
    public async Task PerformanceTest(int iterationCount)
    {
        await using var services = CreateServices();
        var clientPeer = services.GetRequiredService<RpcTestClient>().Single().ClientPeer;
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        await client.Div(1, 1);
        await AssertNoCalls(clientPeer);

        var startedAt = CpuTimestamp.Now;
        for (var i = iterationCount; i > 0; i--)
            if (i != await client.Add(i, 0).ConfigureAwait(false))
                Assert.Fail("Wrong result.");
        var elapsed = startedAt.Elapsed;
        Out.WriteLine($"{iterationCount}: {iterationCount / elapsed.TotalSeconds:F} ops/s");
        await AssertNoCalls(clientPeer);
    }
}
