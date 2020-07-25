using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Tests.Services;
using Stl.Testing;
using Stl.Tests;
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
            await using var serving = await WebSocketHost.ServeAsync();
            var channel = await ConnectToPublisherAsync();
            channel.Writer.Complete();
        }

        [Fact]
        public async Task TimerTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var tp = Services.GetRequiredService<ITimeService>();

            var pub = await Publisher.PublishAsync(_ => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));

            var count = 0;
            using var _ = rep.Computed.AutoUpdate((c, o, _) => {
                Out.WriteLine($"Client: {c.Value}");
                count++;
            });

            await Task.Delay(2000);
            count.Should().BeGreaterThan(2);
        }

        [Fact(Timeout = 120_000)]
        public async Task NoConnectionTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var tp = Services.GetRequiredService<ITimeService>();

            var pub = await Publisher.PublishAsync(_ => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>("NoPublisher", pub.Id);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().ThrowAsync<WebSocketException>();
        }

        [Fact(Timeout = 120_000)]
        public async Task DropReconnectTest()
        {
            if (TestRunnerInfo.GitHub.IsActionRunning)
                // TODO: Fix intermittent failures on GitHub
                return;

            var serving = await WebSocketHost.ServeAsync();
            var tp = Services.GetRequiredService<ITimeService>();

            Debug.WriteLine("0");
            var pub = await Publisher.PublishAsync(_ => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            Debug.WriteLine("1");
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            Debug.WriteLine("2");
            var state = Replicator.GetPublisherConnectionState(pub.Publisher.Id);
            state.IsConsistent.Should().BeFalse();
            Debug.WriteLine("3");
            state = await state.UpdateAsync(false);
            Debug.WriteLine("4");
            state.Should().Be(Replicator.GetPublisherConnectionState(pub.Publisher.Id));
            state.Value.Should().BeTrue();

            Debug.WriteLine("WebServer: stopping.");
            await serving.DisposeAsync();
            Debug.WriteLine("WebServer: stopped.");

            // First try -- should fail w/ WebSocketException or ChannelClosedException
            Debug.WriteLine("5");
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().ThrowAsync<Exception>();
            Debug.WriteLine("6");
            state.Should().Be(Replicator.GetPublisherConnectionState(pub.Publisher.Id));
            state = await state.UpdateAsync(false);
            Debug.WriteLine("7");
            state.Should().Be(Replicator.GetPublisherConnectionState(pub.Publisher.Id));
            state.Error.Should().BeAssignableTo<Exception>();

            // Second try -- should fail w/ WebSocketException
            Debug.WriteLine("8");
            await rep.Computed.UpdateAsync(false).AsAsyncFunc()
                .Should().ThrowAsync<WebSocketException>();
            Debug.WriteLine("9");
            rep.UpdateError.Should().BeOfType<WebSocketException>();
            state = await state.UpdateAsync(false);
            Debug.WriteLine("10");
            state.Error.Should().BeOfType<WebSocketException>();

            Debug.WriteLine("WebServer: starting.");
            serving = await WebSocketHost.ServeAsync();
            await Task.Delay(1000);
            Debug.WriteLine("WebServer: started.");

            Debug.WriteLine("11");
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            Debug.WriteLine("12");
            state = await state.UpdateAsync(false);
            Debug.WriteLine("13");
            state.Value.Should().BeTrue();

            Debug.WriteLine("100");
            await serving.DisposeAsync();
            Debug.WriteLine("101");
        }
    }
}
