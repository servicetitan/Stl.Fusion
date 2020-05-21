using System;
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
        public async Task BasicServiceTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            await using var client = NewWebSocketClient();
            var clientTask = client.RunAsync();
        }

        [Fact]
        public async Task TimerTest()
        {
            await using var serving = await WebSocketServer.ServeAsync();
            await using var client = NewWebSocketClient();
            var clientTask = client.RunAsync();

            var tp = Container.Resolve<ITimeProvider>();
            var pub = await Computed.PublishAsync(Publisher, () => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>(pub!.Publisher.Id, pub.Id);
            await rep.RequestUpdateAsync();
            // await rep.RequestUpdateAsync().AsAsyncFunc()
            //     .Should().CompleteWithinAsync(TimeSpan.FromSeconds(2));

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
            await using var client = NewWebSocketClient();
            var clientTask = client.RunAsync();

            var tp = Container.Resolve<ITimeProvider>();
            var pub = await Computed.PublishAsync(Publisher, () => tp.GetTimeAsync());
            var rep = Replicator.GetOrAdd<DateTime>("NoPublisher", pub.Id);
            await rep.RequestUpdateAsync();
        }
    }
}
