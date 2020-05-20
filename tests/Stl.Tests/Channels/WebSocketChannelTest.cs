using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Red;
using Stl.Channels;
using Stl.Fusion.Bridge.Messages;
using Stl.Hosting.Plugins;
using Stl.Net;
using Stl.Plugins;
using Stl.Plugins.Extensions.Hosting;
using Stl.Serialization;
using Stl.Testing;
using Stl.Tests.Channels;
using Stl.Tests.Hosting;
using Stl.Tests.Hosting.Plugins;
using Xunit;
using Xunit.Abstractions;

[assembly: Plugin(typeof(WebSocketChannelTest.Plugin))]

namespace Stl.Tests.Channels
{
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
            var mChannel = wsChannel.WithSerializer(new JsonNetSerializer<Message>());

            // Actual test
            await mChannel.Writer.WriteAsync(new PublicationStateChangedMessage<int>() { MessageIndex = 100 });
            var m = await mChannel.Reader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<int>>().Which.MessageIndex.Should().Be(100);
            await mChannel.Writer.WriteAsync(new PublicationDisposedMessage());
            await mChannel.Reader.AssertCompletedAsync();
        }

        [Fact]
        public async Task RedHttpServerTest()
        {
            var serverUri = WebTestEx.GetRandomLocalUri();
            var server = new RedHttpServer(serverUri.Port);
            server.WebSocket("/ws", async (req, wsd) => {
                await ProcessServerWebSocketAsync(wsd.WebSocket);
                return HandlerType.Final;
            });
            server.Start();

            // Connecting
            using var ws = new ClientWebSocket();
            var clientUri = new Uri($"{serverUri}ws").ToWss();
            await ws.ConnectAsync(clientUri, CancellationToken.None);
            var wsChannel = new WebSocketChannel(ws);
            var mChannel = wsChannel.WithSerializer(new JsonNetSerializer<Message>());

            // Actual test
            await mChannel.Writer.WriteAsync(new PublicationStateChangedMessage<int>() { MessageIndex = 100 });
            var m = await mChannel.Reader.AssertReadAsync();
            m.Should().BeOfType<PublicationStateChangedMessage<int>>().Which.MessageIndex.Should().Be(100);
            await mChannel.Writer.WriteAsync(new PublicationDisposedMessage());
            await mChannel.Reader.AssertCompletedAsync();
            // await mChannel.Reader.AssertCompletedAsync(TimeSpan.FromMinutes(10));

            await server.StopAsync();
        }

        private static async Task ProcessServerWebSocketAsync(
            WebSocket webSocket, CancellationToken cancellationToken = default)
        {
            await using var wsChannel = new WebSocketChannel(webSocket);
            var mChannel = wsChannel.WithSerializer(new JsonNetSerializer<Message>());
            await Task.Run(async () => {
                await foreach (var m in mChannel.Reader.ReadAllAsync()) {
                    if (m is PublicationDisposedMessage)
                        break;
                    await mChannel.Writer.WriteAsync(m);
                }
                mChannel.Writer.TryComplete();
            });
        }
    }
}
