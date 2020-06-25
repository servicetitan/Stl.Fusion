using System;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Stl.Channels;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Hosting.Plugins;
using Stl.Net;
using Stl.Plugins;
using Stl.Testing;
using Stl.Tests.Channels;
using Stl.Tests.Hosting;
using Stl.Tests.Hosting.Plugins;
using Xunit;
using Xunit.Abstractions;

[assembly: Plugin(typeof(WebSocketChannelTest.Plugin))]

namespace Stl.Tests.Channels
{
    [Category(nameof(TimeSensitiveTests))]
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

        [Fact(Skip = "Failed test, check this later")]
        public async Task KestrelTest()
        {
            Host.Should().NotBeNull();

            // Connecting
            using var ws = new ClientWebSocket();
            var uri = new Uri($"{HostUrl}ws").ToWss();
            await ws.ConnectAsync(uri, CancellationToken.None);
            var wsChannel = new WebSocketChannel(ws);
            var mChannel = wsChannel.WithSerializers(WebSocketChannelProvider.Options.DefaultChannelSerializerPairFactory());

            // Actual test
            await mChannel.Writer.WriteAsync(new PublicationStateChangedMessage<int>() { MessageIndex = 100 });
            var m = await mChannel.Reader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<int>>().Which.MessageIndex.Should().Be(100);
            await mChannel.Writer.WriteAsync(new PublicationAbsentsMessage());
            await mChannel.Reader.AssertCompletedAsync();
        }

        private static async Task ProcessServerWebSocketAsync(
            WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            await using var wsChannel = new WebSocketChannel(webSocket);
            var mChannel = wsChannel.WithSerializers(WebSocketChannelProvider.Options.DefaultChannelSerializerPairFactory());
            await Task.Run(async () => {
                await foreach (var m in mChannel.Reader.ReadAllAsync()) {
                    if (m is PublicationAbsentsMessage)
                        break;
                    await mChannel.Writer.WriteAsync(m);
                }
                mChannel.Writer.TryComplete();
            });
        }
    }
}
