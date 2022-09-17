using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class WebSocketTest : FusionTestBase
{
    public WebSocketTest(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options) { }

    protected override void ConfigureServices(IServiceCollection services, bool isClient = false)
    {
        // We need the same publisher Id here for DropReconnectTest
        services.AddSingleton(new PublisherOptions() { Id = "p" });
        base.ConfigureServices(services, isClient);
    }

    [Fact]
    public async Task ConnectToPublisherTest()
    {
        await using var serving = await WebHost.Serve();
        var channel = await ConnectToPublisher();
        channel.Writer.Complete();
    }

    [Fact]
    public async Task TimerTest()
    {
        await using var serving = await WebHost.Serve();
        var publisher = WebServices.GetRequiredService<IPublisher>();
        var replicator = ClientServices.GetRequiredService<IReplicator>();
        var tp = WebServices.GetRequiredService<ITimeService>();

        var pub = await publisher.Publish(() => tp.GetTime());
        var rep = replicator.GetOrAdd<DateTime>(pub.Ref);
        await rep.RequestUpdate().AsAsyncFunc()
            .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));

        var count = 0;
        using var state = WebServices.StateFactory().NewComputed<DateTime>(
            UpdateDelayer.Instant,
            async (_, ct) => await rep.Computed.Use(ct));
        state.Updated += (s, _) => {
            Out.WriteLine($"Client: {s.Value}");
            count++;
        };

        await TestExt.WhenMet(
            () => count.Should().BeGreaterThan(2),
            TimeSpan.FromSeconds(5));
    }

    [Fact(Timeout = 120_000)]
    public async Task NoConnectionTest()
    {
        await using var serving = await WebHost.Serve();
        var publisher = WebServices.GetRequiredService<IPublisher>();
        var replicator = ClientServices.GetRequiredService<IReplicator>();
        var time = WebServices.GetRequiredService<ITimeService>();

        var pub = await publisher.Publish(() => time.GetTime());

        var rep1 = replicator.GetOrAdd<DateTime>(("NoPublisher", pub.Id));
        rep1.Computed.IsConsistent().Should().BeFalse();
        var updateTask1 = rep1.RequestUpdate();

        var psi2 = new PublicationStateInfo<DateTime>(
            ("NoPublisher1", pub.Id),
            new LTag(123),
            true,
            pub.State.Computed.Value);
        var rep2 = replicator.GetOrAdd(psi2);
        rep2.Computed.IsConsistent().Should().BeTrue();
        var updateTask2 = rep2.RequestUpdate();

        await Delay(30);

        // No publisher = no update
        updateTask1.IsCompleted.Should().BeFalse();
        updateTask2.IsCompleted.Should().BeFalse();
        // And state should be the same (shouldn't reset to inconsistent)
        rep1.Computed.IsConsistent().Should().BeFalse();
        rep2.Computed.IsConsistent().Should().BeTrue();
    }

    [SkipOnGitHubFact(Timeout = 120_000)]
    public async Task DropReconnectTest()
    {
        var serving = await WebHost.Serve(false);
        var replicator = ClientServices.GetRequiredService<IReplicator>();
        var kvsClient = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();

        Debug.WriteLine("0");
        var kvs = WebServices.GetRequiredService<IKeyValueService<string>>();
        await kvs.Set("a", "b");
        var c = (ReplicaMethodComputed<string>) await Computed.Capture(() => kvsClient.Get("a"));
        c.Value.Should().Be("b");
        c.IsConsistent().Should().BeTrue();

        Debug.WriteLine("1");
        await c.Replica!.RequestUpdate().AsAsyncFunc()
            .Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
        c.IsConsistent().Should().BeTrue();

        Debug.WriteLine("2");
        var cs = replicator.GetPublisherConnectionState(c.Replica.PublicationRef.PublisherId);
        cs.Value.Should().BeTrue();
        cs.Computed.IsConsistent().Should().BeTrue();
        await cs.Recompute();
        Debug.WriteLine("3");
        cs.Value.Should().BeTrue();
        cs.Computed.IsConsistent().Should().BeTrue();
        var cs1 = replicator.GetPublisherConnectionState(c.Replica.PublicationRef.PublisherId);
        cs1.Should().BeSameAs(cs);

        Debug.WriteLine("WebServer: stopping.");
        await serving.DisposeAsync();
        Debug.WriteLine("WebServer: stopped.");

        // First try -- should fail w/ WebSocketException or ChannelClosedException
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("b");
        Debug.WriteLine("4");

        await cs.Update();
        cs.Error.Should().BeAssignableTo<Exception>();
        cs.Computed.IsConsistent().Should().BeTrue();
        var updateTask = c.Replica.RequestUpdate();
        updateTask.IsCompleted.Should().BeFalse();
        Debug.WriteLine("5");

        await kvs.Set("a", "c");
        await Delay(0.1);
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("b");
        Debug.WriteLine("6");

        Debug.WriteLine("WebServer: starting.");
        serving = await WebHost.Serve();
        await Delay(1);
        Debug.WriteLine("WebServer: started.");

        await TestExt.WhenMet(
            () => cs.Error.Should().BeNull(),
            TimeSpan.FromSeconds(30));
        Debug.WriteLine("7");

        await Delay(1);
        updateTask.IsCompleted.Should().BeTrue();
        c = (ReplicaMethodComputed<string>) await c.Update();
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("c");

        await serving.DisposeAsync();
    }
}
