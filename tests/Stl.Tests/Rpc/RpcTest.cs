using Stl.Rpc;
using Stl.Rpc.Infrastructure;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcTest : TestBase
{
    public RpcTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServerServices();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(10, 2)).Should().Be(5);
        (await client.Div(null, 2)).Should().Be(null);
        await Assert.ThrowsAsync<DivideByZeroException>(
            () => client.Div(1, 0));
    }

    [Fact]
    public async Task DelayTest()
    {
        var services = CreateServerServices();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();
        var startedAt = CpuTimestamp.Now;
        await client.Delay(TimeSpan.FromMilliseconds(200));
        startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(100, 500);

        {
            using var cts = new CancellationTokenSource(1);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(0, 500);
        }

        {
            using var cts = new CancellationTokenSource(500);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(300, 1000);
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
        var services = CreateServerServices();
        var client = services.GetRequiredService<ISimpleRpcServiceClient>();

        var startedAt = CpuTimestamp.Now;
        for (var i = iterationCount; i > 0; i--)
            await client.Div(1, 2);
        var elapsed = startedAt.Elapsed;
        Out.WriteLine($"{iterationCount}: {iterationCount / elapsed.TotalSeconds:F} ops/s");
    }

    private IServiceProvider CreateServerServices()
    {
        var services = new ServiceCollection();
        var channelPair = CreateRpcChannelPair();
        services.AddSingleton(channelPair);
        services.AddSingleton<SimpleRpcService>();

        var rpc = services.AddRpc();
        var serverPeerName = new Symbol("server");
        rpc.HasPeerFactory(c => {
            var hub = c.RpcHub();
            return name => new RpcPeer(hub, name) {
                LocalServiceFilter = name == serverPeerName
                    ? _ => true
                    : _ => false
            };
        });
        rpc.HasPeerConnector((peer, _) => {
            var c = peer.Hub.Services;
            var cp = c.GetRequiredService<ChannelPair<RpcMessage>>();
            var channel = peer.Name == serverPeerName ? cp.Channel1 : cp.Channel2;
            return Task.FromResult(channel);
        });
        rpc.AddService<ISimpleRpcService>(cfg => cfg.With<SimpleRpcService, ISimpleRpcServiceClient>());

        var c = services.BuildServiceProvider();
        _ = c.RpcHub().GetPeer(serverPeerName);
        return c;
    }

    private ChannelPair<RpcMessage> CreateRpcChannelPair()
        => CreateChannelPair<RpcMessage>();

    private ChannelPair<T> CreateChannelPair<T>()
    {
        var options = new UnboundedChannelOptions() {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
        };
        return ChannelPair.CreateTwisted(
            Channel.CreateUnbounded<T>(options),
            Channel.CreateUnbounded<T>(options));
    }
}
