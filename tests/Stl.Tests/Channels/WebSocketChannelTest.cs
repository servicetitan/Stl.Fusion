using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Hosting.Plugins;
using Stl.Net;
using Stl.Plugins;
using Stl.Testing;
using Stl.Tests.Channels;
using Stl.Tests.Hosting;
using Stl.Tests.Hosting.Plugins;
using Stl.Text;
using Xunit;
using Xunit.Abstractions;

[assembly: Plugin(typeof(WebSocketChannelTest.Plugin))]

namespace Stl.Tests.Channels
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class WebSocketChannelTest : MiniHostTestBase
    {
        public class Plugin : IConfigureWebAppPlugin, IMiniHostPlugin
        {
            public void Use(ConfigureWebAppPluginInvoker invoker)
            {
                var app = invoker.AppBuilder;
                app.Use(WebSocketMiddleware);
            }

            private async Task WebSocketMiddleware(HttpContext ctx, Func<Task> next)
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

        }

        public WebSocketChannelTest(ITestOutputHelper @out) : base (@out)
        {
            HostProvider = new TestMiniHostProvider(@out, typeof(Plugin));
        }

        [Fact]
        public async Task KestrelTest()
        {
            Host.Should().NotBeNull();

            // Connecting
            using var ws = new ClientWebSocket();
            var uri = new Uri($"{HostUrl}ws").ToWss();
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
