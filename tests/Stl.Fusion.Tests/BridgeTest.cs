using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class BridgeTest : FusionTestBase
{
    public BridgeTest(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options) { }

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
        var ctp = ClientServices.GetRequiredService<IClientTimeService>();

        var cTime = await Computed.Capture(() => ctp.GetTime()).AsTask().WaitAsync(TimeSpan.FromMinutes(1));
        var count = 0;
        using var state = WebServices.StateFactory().NewComputed<DateTime>(
            FixedDelayer.Instant,
            async (_, ct) => await ctp.GetTime(ct));
        state.Updated += (s, _) => {
            Out.WriteLine($"Client: {s.Value}");
            count++;
        };

        await TestExt.WhenMet(
            () => count.Should().BeGreaterThan(2),
            TimeSpan.FromSeconds(5));
    }

    [SkipOnGitHubFact(Timeout = 120_000)]
    public async Task DropReconnectTest1()
    {
        var serving = await WebHost.Serve(false);
        var replicator = ClientServices.GetRequiredService<IReplicator>();
        var kvsClient = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();

        Debug.WriteLine("0");
        var kvs = WebServices.GetRequiredService<IKeyValueService<string>>();
        await kvs.Set("a", "b");
        var c = (ReplicaMethodComputed<string>) await Computed.Capture(() => kvsClient.Get("a"));
        var pub = c.State.PublicationRef;
        c.Value.Should().Be("b");
        c.IsConsistent().Should().BeTrue();

        Debug.WriteLine("1");
        await Assert.ThrowsAsync<TimeoutException>(async () => {
            // RequestUpdate should wait until the invalidation in this case,
            // and since there is none, it should wait indefinitely.
            await c.Replica!.RequestUpdate().WaitAsync(TimeSpan.FromSeconds(3));
        });
        c.IsConsistent().Should().BeTrue();

        Debug.WriteLine("2");
        var isConnected = replicator.GetPublisherConnectionState(c.Replica!.PublicationRef.PublisherId);
        isConnected.Value.Should().BeTrue();
        isConnected.Computed.IsConsistent().Should().BeTrue();
        Debug.WriteLine("3");
        var cs1 = replicator.GetPublisherConnectionState(c.Replica.PublicationRef.PublisherId);
        cs1.Should().BeSameAs(isConnected);

        Debug.WriteLine("WebServer: stopping.");
        await serving.DisposeAsync();
        await isConnected.Changes()
            .FirstAsync(c1 => c1.Error != null).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10));
        Debug.WriteLine("WebServer: stopped.");

        // Nothing should be changed
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("b");
        Debug.WriteLine("4");

        var updateTask = c.Replica.RequestUpdate();
        await Delay(0.5);
        updateTask.IsCompleted.Should().BeFalse();
        Debug.WriteLine("5");

        await kvs.Set("a", "c");
        await Delay(0.1);
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("b");
        Debug.WriteLine("6");

        Debug.WriteLine("WebServer: starting.");
        serving = await WebHost.Serve();
        await isConnected.Changes()
            .FirstAsync(c1 => c1.ValueOrDefault).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10));
        Debug.WriteLine("WebServer: started.");

        await Delay(1);
        updateTask.IsCompleted.Should().BeTrue();
        c = (ReplicaMethodComputed<string>) await c.Update();
        c.IsConsistent().Should().BeTrue();
        if (c.State.PublicationRef == pub)
            c.Value.Should().Be("c");
        else
            c.Value.Should().BeNull();

        Debug.WriteLine("Disposing");
        await serving.DisposeAsync();
    }

    [SkipOnGitHubFact(Timeout = 120_000)]
    public async Task DropReconnectTest2()
    {
        var kvsClient = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();
        var transientErrorInvalidationDelay = ComputedOptions.ReplicaDefault.TransientErrorInvalidationDelay;

        Debug.WriteLine("0");
        var kvs = WebServices.GetRequiredService<IKeyValueService<string>>();
        await kvs.Set("a", "b");
        var c = (ReplicaMethodComputed<string>) await Computed.Capture(() => kvsClient.Get("a"));
        c.Options.TransientErrorInvalidationDelay
            .Should().Be(transientErrorInvalidationDelay);
        c.HasError.Should().BeTrue();
        c.State.PublicationRef.IsNone.Should().BeTrue();
        c.IsConsistent().Should().BeTrue();

        await Delay((transientErrorInvalidationDelay + TimeSpan.FromSeconds(0.5)).TotalSeconds);
        c.IsConsistent().Should().BeFalse();

        Debug.WriteLine("WebServer: starting.");
        var serving = await WebHost.Serve();
        Debug.WriteLine("WebServer: started.");

        c = (ReplicaMethodComputed<string>)await c.Update();
        c.IsConsistent().Should().BeTrue();
        c.Value.Should().Be("b");

        Debug.WriteLine("Disposing");
        await serving.DisposeAsync();
    }
}
