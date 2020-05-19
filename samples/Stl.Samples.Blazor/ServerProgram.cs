using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Red;
using Stl.Channels;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.IO;
using Stl.Net;
using Stl.Samples.Blazor.Services;
using Stl.Serialization;

namespace Stl.Samples.Blazor
{
    public class ServerProgram
    {
        public Task RunAsync(string[] args)
        {
            var myLocation = (PathString) Assembly.GetExecutingAssembly().Location;
            var baseDir = myLocation.GetDirectoryPath();
            var wwwRoot = baseDir & "wwwroot";
            var server = new RedHttpServer {
                ConfigureServices = services => {
                    services.AddLogging(logging => {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddDebug();
                    });
                    services.AddFusion();
                    services.AddComputedProvider<ITimeProvider, TimeProvider>();
                },
                ConfigureApplication = app => {
                    var wwwRootFileProvider = new PhysicalFileProvider(wwwRoot);
                    app.UseDefaultFiles();
                    app.UseStaticFiles(new StaticFileOptions {
                        FileProvider = wwwRootFileProvider,
                        DefaultContentType = "application/octet-stream",
                        ServeUnknownFileTypes = true,
                    });
                }
            };
            server.WebSocket("/ws", async (req, wsd) => {
                var services = req.Context.AspNetContext.RequestServices;
                var publisher = services.GetRequiredService<IPublisher>();
                await using var wsChannel = new WebSocketChannel(wsd.WebSocket);
                var channel = wsChannel
                    .WithSerializer(new JsonNetSerializer<Message>())
                    .WithId(req.Queries["clientId"]);
                publisher.ChannelHub.Attach(channel);
                await wsChannel.ReaderTask.ConfigureAwait(false);
                return HandlerType.Final;
            });
            server.Get("services/time", async (req, res) => {
                var services = req.Context.AspNetContext.RequestServices;
                var publisher = services.GetRequiredService<IPublisher>();
                var timeProvider = services.GetRequiredService<ITimeProvider>();
                var publication = await Computed
                    .PublishAsync(publisher, () => timeProvider.GetTimeAsync())
                    .ConfigureAwait(false);
                res.SendJson(new PublicationPublishedMessage() {
                    PublicationId = publication.Id,
                    PublisherId = publisher.Id,
                });
                return HandlerType.Final;
            });
            return server.RunAsync();
        }
    }
}
