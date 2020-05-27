using System;
using System.Net.WebSockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Testing;
using Stl.Tests.Fusion.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class WebSocketTest : FusionTestBase
    {
        public WebSocketTest(ITestOutputHelper @out, FusionTestOptions? options = null) 
            : base(@out, options) { }

        [Fact]
        public async Task ConnectToPublisherTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            var channel = await ConnectToPublisherAsync();
            channel.Writer.Complete();
        }

        [Fact]
        public async Task TimerTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            var tp = Container.Resolve<ITimeProvider>();

            var pub = await Computed.PublishAsync(Publisher, () => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));

            var count = 0;
            using var _ = rep.Computed.AutoUpdate((c, o, _) => {
                Out.WriteLine($"Client: {c.Value}");
                count++;
            });

            await Task.Delay(2000);
            count.Should().BeGreaterThan(4);
        }

        [Fact]
        public async Task NoConnectionTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            var tp = Container.Resolve<ITimeProvider>();

            var pub = await Computed.PublishAsync(Publisher, () => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>("NoPublisher", pub.Id);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().ThrowAsync<WebSocketException>();
        }

        [Fact]
        public async Task DropReconnectTest()
        {
            var serving = await WebSocketServer.ServeAsync();
            var tp = Container.Resolve<ITimeProvider>();

            var pub = await Computed.PublishAsync(Publisher, () => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            var state = await Replicator.GetPublisherChannelState(pub.Publisher.Id).UpdateAsync();
            state.Value.Should().BeTrue();
                
            await serving.DisposeAsync();
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().ThrowAsync<ChannelClosedException>();
            state = await state.UpdateAsync();
            state.Error.Should().BeOfType<ChannelClosedException>();

            serving = await WebSocketServer.ServeAsync();
            await Task.Delay(1000);
            await rep.RequestUpdateAsync().AsAsyncFunc()
                .Should().CompleteWithinAsync(TimeSpan.FromMinutes(1));
            state = await state.UpdateAsync();
            state.Value.Should().BeTrue();

            await serving.DisposeAsync();
        }
    }
}
