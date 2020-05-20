using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Stl.Fusion;
using Stl.IO;
using Stl.Samples.Blazor.Common.Services;
using Stl.Samples.Blazor.Server.Services;

namespace Stl.Samples.Blazor.Server
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var myLocation = (PathString) Assembly.GetExecutingAssembly().Location;
            var baseDir = myLocation.GetDirectoryPath();
            var wwwRoot = baseDir & "../Stl.Samples.Blazor/wwwroot";
            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(wwwRoot)
                .ConfigureServices(services => {
                    services.AddCors(o => o.AddPolicy("AllowAll", builder => {
                        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }));
                    services.AddLogging(logging => {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                        logging.AddDebug();
                    });
                    services.AddFusion();
                    services.AddComputedProvider<ITimeProvider, TimeProvider>();

                    // Web
                    services.AddScoped<PublisherMiddleware>();
                    services.AddRouting(options => options.LowercaseUrls = true);
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
                    services.AddHostedService<ApplicationPartsLogger>();
                })
                .ConfigureWebHost(builder => {
                    builder
                        .UseKestrel()
                        .UseUrls($"http://localhost:{Settings.ServerPort}");
                    builder.Configure((ctx, app) => {
                        if (ctx.HostingEnvironment.IsDevelopment())
                            app.UseDeveloperExceptionPage();

                        // Static + Swagger
                        app.UseDefaultFiles();
                        app.UseStaticFiles(new StaticFileOptions {
                            FileProvider = new PhysicalFileProvider(wwwRoot),
                            DefaultContentType = "application/octet-stream",
                            ServeUnknownFileTypes = true,
                        });
                        app.UseSwagger();
                        app.UseSwaggerUI(c => {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                        });

                        // WebSockets
                        app.UseWebSockets(new WebSocketOptions() { ReceiveBufferSize = 16_384 });
                        app.UseMiddleware<PublisherMiddleware>();

                        // API controllers
                        app.UseRouting();
                        app.UseCors("AllowAll");
                        app.UseEndpoints(endpoints => {
                            endpoints.MapControllers();
                        });
                    });
                })
                .Build();
            return host.RunAsync();
        }
    }
}
