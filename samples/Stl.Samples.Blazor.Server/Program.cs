using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using RestEase;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Stl.IO;
using Stl.Samples.Blazor.Common.Services;
using Stl.Samples.Blazor.Server.Services;

namespace Stl.Samples.Blazor.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var myLocation = (PathString) Assembly.GetExecutingAssembly().Location;
            var baseDir = myLocation.DirectoryPath;
            var wwwRoot = baseDir & "../Stl.Samples.Blazor.Client/wwwroot";

            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(wwwRoot)
                .ConfigureServices(services => {
                    services.AddLogging(logging => {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddDebug();
                    });

                    // DbContext & related services
                    var appTempDir = PathEx.GetApplicationTempDirectory("", true);
                    var dbPath = appTempDir & "Chat.db";
                    services
                        .AddEntityFrameworkSqlite()
                        .AddDbContextPool<ChatDbContext>(builder => {
                            builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
                        });
                    services.AddSingleton<ChatDbContextPool>();

                    // Fusion services
                    services.AddSingleton(new Publisher.Options() { Id = Settings.PublisherId });
                    services.AddFusionWebSocketServer();
                    services.AddComputedService<ITimeService, TimeService>();
                    services.AddComputedService<IChatService, ChatService>();
                    services.AddSingleton(c => new RestClient(new Uri("https://uzby.com/api.php"))
                        .For<IUzbyClient>());
                    services.AddSingleton(c => new RestClient(new Uri("https://api.forismatic.com/api/1.0/"))
                        .For<IForismaticClient>());
                    services.AddComputedService<IScreenshotService, ScreenshotService>();

                    // Web
                    services.AddRouting();
                    services.AddControllers()
                        .AddApplicationPart(Assembly.GetExecutingAssembly());
                    services.AddMvc()
                        .AddNewtonsoftJson()
                        .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

                    // Swagger & debug tools
                    services.AddSwaggerGen(c => {
                        c.SwaggerDoc("v1", new OpenApiInfo {
                            Title = "Stl.Sample.Blazor.Server API", Version = "v1"
                        });
                    });
                })
                .ConfigureWebHostDefaults(builder => {
                    builder.Configure((ctx, app) => {
                        if (ctx.HostingEnvironment.IsDevelopment()) {
                            app.UseDeveloperExceptionPage();
                            app.UseWebAssemblyDebugging();
                        }

                        // Static + Swagger
                        var staticFileOptions = new StaticFileOptions {
                            FileProvider = new PhysicalFileProvider(wwwRoot),
                            DefaultContentType = "application/octet-stream",
                            ServeUnknownFileTypes = true,
                        };
                        app.UseDefaultFiles();
                        app.UseStaticFiles(staticFileOptions);
                        app.UseSwagger();
                        app.UseSwaggerUI(c => {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                        });
                        
                        // Stl.Fusion server
                        app.UseFusionWebSocketServer(true);

                        // API controllers
                        app.UseRouting();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapControllers();
                            endpoints.MapFallbackToFile("index.html", staticFileOptions);
                        });
                    });
                })
                .Build();

            // Ensure the DB is created
            using (var scope = host.Services.CreateScope()) {
                var services = scope.ServiceProvider;
                var chatDbContext = services.GetRequiredService<ChatDbContext>();
                await chatDbContext.Database.EnsureCreatedAsync();
            }

            await host.RunAsync();
        }
    }
}
