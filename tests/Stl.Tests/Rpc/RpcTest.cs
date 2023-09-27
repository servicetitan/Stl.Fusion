using Stl.OS;
using Stl.Rpc;
using Stl.Testing.Collections;

namespace Stl.Tests.Rpc;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RpcWebSocketTest(ITestOutputHelper @out) : RpcTestBase(@out)
{
    protected override void ConfigureServices(IServiceCollection services, bool isClient)
    {
        base.ConfigureServices(services, isClient);
        var rpc = services.AddRpc();
        var commander = services.AddCommander();
        if (isClient) {
            rpc.AddClient<ITestRpcServiceClient>();
            rpc.Service<ITestRpcServiceClient>().HasName(nameof(ITestRpcService));
            commander.AddService<ITestRpcServiceClient>();
            rpc.AddClient<ITestRpcBackend, ITestRpcBackendClient>();
            commander.AddService<ITestRpcBackendClient>();
        }
        else {
            rpc.AddServer<ITestRpcService, TestRpcService>();
            commander.AddService<TestRpcService>();
            rpc.AddServer<ITestRpcBackend, TestRpcBackend>();
            commander.AddService<TestRpcBackend>();
            services.AddSingleton<RpcPeerFactory>(c => static (hub, peerRef) => {
                return peerRef.IsServer
                    ? new RpcServerPeer(hub, peerRef) {
                        LocalServiceFilter = static serviceDef
                            => !serviceDef.IsBackend || serviceDef.Type == typeof(ITestRpcBackend),
                    }
                    : new RpcClientPeer(hub, peerRef);
            });
        }
    }

    [Fact]
    public async Task BasicTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var client = services.GetRequiredService<ITestRpcServiceClient>();
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(6, 2)).Should().Be(3);
        (await client.Div(10, 2)).Should().Be(5);
        (await client.Div(null, 2)).Should().Be(null);
        await Assert.ThrowsAsync<DivideByZeroException>(
            () => client.Div(1, 0));

        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task MulticallTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var tasks = new Task<int?>[100];
        for (var i = 0; i < tasks.Length; i++)
            tasks[i] = client.Add(0, i);
        var results = await Task.WhenAll(tasks);
        for (var i = 0; i < results.Length; i++)
            results[i].Should().Be((int?)i);

        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task CommandTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var commander = services.Commander();
        await commander.Call(new HelloCommand("ok"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => commander.Call(new HelloCommand("error")));

        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task NoWaitTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        // We need to make sure the connection is there before the next call
        await client.Add(1, 1);

        await client.MaybeSet("a", "b");
        await TestExt.WhenMetAsync(async () => {
            var result = await client.Get("a");
            result.Should().Be("b");
        }, TimeSpan.FromSeconds(1));

        await client.MaybeSet("a", "c");
        await TestExt.WhenMetAsync(async () => {
            var result = await client.Get("a");
            result.Should().Be("c");
        }, TimeSpan.FromSeconds(1));

        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task DelayTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();
        var startedAt = CpuTimestamp.Now;
        await client.Delay(TimeSpan.FromMilliseconds(200));
        startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(100, 500);
        await AssertNoCalls(peer);

        {
            using var cts = new CancellationTokenSource(1);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(0, 500);
            await AssertNoCalls(peer);
        }

        {
            using var cts = new CancellationTokenSource(500);
            startedAt = CpuTimestamp.Now;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.Delay(TimeSpan.FromHours(1), cts.Token));
            startedAt.Elapsed.TotalMilliseconds.Should().BeInRange(300, 1000);
            await AssertNoCalls(peer);
        }
    }

    [Fact]
    public async Task PolymorphTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = ClientServices.GetRequiredService<ITestRpcServiceClient>();
        var backendClient = ClientServices.GetRequiredService<ITestRpcBackendClient>();

        var t = new Tuple<int>(1);
        var t1 = await backendClient.Polymorph(t);
        t1.Should().Be(t);
        t1.Should().NotBeSameAs(t);

        await Assert.ThrowsAnyAsync<Exception>(
            async () => await client.PolymorphArg(new Tuple<int>(1)));
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await client.PolymorphResult(2));

        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task EndpointNotFoundTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        try {
            await client.NoSuchMethod(1, 2, 3, 4);
            Assert.Fail("RpcException wasn't thrown.");
        }
        catch (RpcException e) {
            Out.WriteLine(e.Message);
            e.Message.Should().StartWith("Endpoint not found:");
            e.Message.Should().Contain("NoSuchMethod");
            e.Message.Should().Contain("ITestRpcService");
        }

        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task StreamTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var expected1 = Enumerable.Range(0, 500).ToList();
        var stream1 = await client.StreamInt32(expected1.Count);
        (await stream1.ToListAsync()).Should().Equal(expected1);
        await AssertNoCalls(peer);

        var expected2 = Enumerable.Range(0, 500)
            .Select(x => (x & 2) == 0 ? (ITuple)new Tuple<int>(x) : new Tuple<long>(x))
            .ToList();
        var stream2 = await client.StreamTuples(expected2.Count);
        (await stream2.ToListAsync()).Should().Equal(expected2);
        await AssertNoCalls(peer);

        var stream3 = await client.StreamTuples(10, 5);
        (await stream3.Take(5).CountAsync()).Should().Be(5);
        var stream3f = await client.StreamTuples(10, 5);
        try {
            await stream3f.CountAsync();
            Assert.Fail("No exception!");
        }
        catch (Exception e) {
            e.Should().BeOfType<InvalidOperationException>();
        }
        await AssertNoCalls(peer);
    }

    [Fact]
    public async Task StreamInputTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var expected1 = AsyncEnumerable.Range(0, 500);
        (await client.Count(RpcStream.New(expected1))).Should().Be(500);
    }

    [Fact]
    public async Task StreamDebugTest()
    {
        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var tasks = new Task<List<int>>[3];
        for (var i = 0; i < tasks.Length; i++) {
            var taskIndex = i;
            tasks[i] = Task.Run(async () => {
                var stream = await client.StreamInt32(100_000);
                var list = new List<int>();
                await foreach (var j in stream.ConfigureAwait(false)) {
                    if (j % 1000 == 0)
                        Out.WriteLine($"{taskIndex}: {j}");
                    list.Add(j);
                }
                return list;
            }, CancellationToken.None);
        }
        await Task.WhenAll(tasks);
        await AssertNoCalls(peer);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(50_000)]
    public async Task PerformanceTest(int iterationCount)
    {
        // ByteSerializer.Default = MessagePackByteSerializer.Default;
        if (TestRunnerInfo.IsBuildAgent())
            iterationCount = 100;

        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var threadCount = Math.Max(1, HardwareInfo.ProcessorCount / 4);
        var tasks = new Task[threadCount];
        await Run(10); // Warmup
        var elapsed = await Run(iterationCount);

        var totalIterationCount = threadCount * iterationCount;
        Out.WriteLine($"{iterationCount}: {totalIterationCount / elapsed.TotalSeconds:F} ops/s using {threadCount} threads");
        await AssertNoCalls(peer);

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

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(50_000)]
    public async Task StreamPerformanceTest(int itemCount)
    {
        if (TestRunnerInfo.IsBuildAgent())
            itemCount = 100;

        await using var _ = await WebHost.Serve();
        var services = ClientServices;
        var peer = services.RpcHub().GetClientPeer(ClientPeerRef);
        var client = services.GetRequiredService<ITestRpcServiceClient>();

        var threadCount = Math.Max(1, HardwareInfo.ProcessorCount / 2);
        var tasks = new Task[threadCount];
        await Run(10); // Warmup
        var elapsed = await Run(itemCount);

        var totalItemCount = threadCount * itemCount;
        Out.WriteLine($"{itemCount}: {totalItemCount / elapsed.TotalSeconds:F} ops/s using {threadCount} threads");
        await AssertNoCalls(peer);

        async Task<TimeSpan> Run(int count)
        {
            var startedAt = CpuTimestamp.Now;
            for (var threadIndex = 0; threadIndex < threadCount; threadIndex++) {
                tasks[threadIndex] = Task.Run(async () => {
                    var stream = await client.StreamInt32(count);
                    (await stream.CountAsync()).Should().Be(count);
                }, CancellationToken.None);
            }

            await Task.WhenAll(tasks);
            return elapsed = startedAt.Elapsed;
        }
    }
}
