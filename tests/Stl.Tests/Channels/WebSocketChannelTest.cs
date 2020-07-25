using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.Channels;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Net;
using Stl.Testing;
using Stl.Text;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Channels
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class WebSocketChannelServer : TestBase
    {
        public class TestHost : TestWebHostBase
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                async Task WebSocketMiddleware(HttpContext ctx, Func<Task> next)
                {
                    if (ctx.Request.Path == "/ws") {
                        if (ctx.WebSockets.IsWebSocketRequest) {
                            var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                            await ProcessServerWebSocketAsync(ws);
                        }
                        else
                            ctx.Response.StatusCode = 400;
                    }
                    else
                        await next();
                }

                builder.Configure((ctx, app) => {
                    app.UseWebSockets(new WebSocketOptions());
                    app.Use(WebSocketMiddleware);
                });
            }
        }

        public WebSocketChannelServer(ITestOutputHelper @out) : base (@out) { }

        [Fact]
        public async Task KestrelTest()
        {
            using var testHost = new TestHost();
            await using var _ = await testHost.ServeAsync();

            // Connecting
            using var ws = new ClientWebSocket();
            var uri = new Uri($"{testHost.ServerUri}ws").ToWss();
            await ws.ConnectAsync(uri, CancellationToken.None);
            var wsChannel = new WebSocketChannel(ws);
            var defaultOptions = new WebSocketChannelProvider.Options();
            var serializers = defaultOptions.ChannelSerializerPairFactory.Invoke(null!);
            var mChannel = wsChannel.WithSerializers(serializers);

            // Actual test
            await mChannel.Writer.WriteAsync(new SubscribeMessage() { PublisherId = "X" });
            (await mChannel.Reader.AssertReadAsync())
                .Should().BeOfType<PublicationAbsentsMessage>();
            await mChannel.Writer.WriteAsync(new SubscribeMessage());
            await mChannel.Reader.AssertCompletedAsync();
        }

        private static async Task ProcessServerWebSocketAsync(
            WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            await using var wsChannel = new WebSocketChannel(webSocket);
            var defaultOptions = new WebSocketChannelProvider.Options();
            var serializers = defaultOptions.ChannelSerializerPairFactory.Invoke(null!).Swap();
            var mChannel = wsChannel.WithSerializers(serializers);
            await Task.Run(async () => {
                await foreach (var m in mChannel.Reader.ReadAllAsync()) {
                    if (m is SubscribeMessage sm && sm.PublisherId == Symbol.Null)
                        break;
                    await mChannel.Writer.WriteAsync(new PublicationAbsentsMessage());
                }
                mChannel.Writer.TryComplete();
            });
        }
    }
}
