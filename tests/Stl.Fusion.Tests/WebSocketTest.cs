using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Tests.Services;
using Stl.Testing;
using Stl.Testing.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class WebSocketTest : FusionTestBase
    {
        public WebSocketTest(ITestOutputHelper @out, FusionTestOptions? options = null)
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

            var pub = await publisher.Publish(_ => tp.GetTime());
            var rep = replicator.GetOrAdd<DateTime>(pub.Ref);
            await rep.RequestUpdate().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));

            var count = 0;
            using var state = WebServices.StateFactory().NewComputed<DateTime>(
                UpdateDelayer.ZeroDelay,
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
            var tp = WebServices.GetRequiredService<ITimeService>();

            var pub = await publisher.Publish(_ => tp.GetTime());
            var rep = replicator.GetOrAdd<DateTime>(("NoPublisher", pub.Id));
            await rep.RequestUpdate().AsAsyncFunc()
                .Should().ThrowAsync<WebSocketException>();
        }

        [Fact(Timeout = 120_000)]
        public async Task DropReconnectTest()
        {
            if (TestRunnerInfo.IsBuildAgent())
                // TODO: Fix intermittent failures on GitHub
                return;

            var serving = await WebHost.Serve();
            var publisher = WebServices.GetRequiredService<IPublisher>();
            var replicator = ClientServices.GetRequiredService<IReplicator>();
            var tp = Services.GetRequiredService<ITimeService>();

            Debug.WriteLine("0");
            var pub = await publisher.Publish(_ => tp.GetTime());
            var rep = replicator.GetOrAdd<DateTime>(pub.Ref);
            Debug.WriteLine("1");
            await rep.RequestUpdate().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            Debug.WriteLine("2");
            var state = replicator.GetPublisherConnectionState(pub.Publisher.Id);
            state.Computed.IsConsistent().Should().BeTrue();
            Debug.WriteLine("3");
            await state.Computed.Update();
            Debug.WriteLine("4");
            state.Should().Be(replicator.GetPublisherConnectionState(pub.Publisher.Id));
            state.Value.Should().BeTrue();

            Debug.WriteLine("WebServer: stopping.");
            await serving.DisposeAsync();
            Debug.WriteLine("WebServer: stopped.");

            // First try -- should fail w/ WebSocketException or ChannelClosedException
            Debug.WriteLine("5");
            await rep.RequestUpdate().AsAsyncFunc()
                .Should().ThrowAsync<Exception>();
            Debug.WriteLine("6");
            state.Should().Be(replicator.GetPublisherConnectionState(pub.Publisher.Id));
            await state.Computed.Update();
            Debug.WriteLine("7");
            state.Should().Be(replicator.GetPublisherConnectionState(pub.Publisher.Id));
            state.Error.Should().BeAssignableTo<Exception>();

            // Second try -- should fail w/ WebSocketException
            Debug.WriteLine("8");
            await rep.Computed.Update().AsAsyncFunc()
                .Should().ThrowAsync<WebSocketException>();
            Debug.WriteLine("9");
            rep.UpdateError.Should().BeOfType<WebSocketException>();
            await state.Computed.Update();
            Debug.WriteLine("10");
            state.Error.Should().BeOfType<WebSocketException>();

            // The remaining part of this test shouldn't work:
            // since the underlying web host is actually re-created on
            // every Serve call, its endpoints change,
            // and moreover, IPublisher, etc. dies there,
            // so reconnect won't happen in this case.
            //
            // TODO: Add similar test relying on Replica Services.

            /*
            Debug.WriteLine("WebServer: starting.");
            serving = await WebHost.ServeAsync();
            await Task.Delay(1000);
            Debug.WriteLine("WebServer: started.");

            Debug.WriteLine("11");
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            Debug.WriteLine("12");
            await state.Computed.UpdateAsync(false);
            Debug.WriteLine("13");
            state.Value.Should().BeTrue();

            Debug.WriteLine("100");
            await serving.DisposeAsync();
            Debug.WriteLine("101");
            */
        }
    }
}
