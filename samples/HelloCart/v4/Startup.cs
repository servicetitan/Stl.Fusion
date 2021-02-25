using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samples.HelloCart.V2;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Operations.Internal;
using Stl.Fusion.Server;
using Stl.IO;

namespace Samples.HelloCart.V4
{
    public class Startup
    {
        public static Uri BaseUri { get; } = new("http://localhost:7005");
        public static Uri ApiBaseUri { get; } = new($"{BaseUri}api/");

        private IConfiguration Cfg { get; }
        private IWebHostEnvironment Env { get; }

        public Startup(IConfiguration cfg, IWebHostEnvironment environment)
        {
            Cfg = cfg;
            Env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureLogging(services);
            services.AddFusion(fusion => {
                fusion.AddComputeService<IProductService, DbProductService>();
                fusion.AddComputeService<ICartService, DbCartService>();
            });

            // Add AppDbContext & related services
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            var dbPath = appTempDir & "HelloCart_v01.db";
            services.AddDbContextFactory<AppDbContext>(b => {
                b.UseSqlite($"Data Source={dbPath}");
                b.EnableSensitiveDataLogging();
            });
            services.AddDbContextServices<AppDbContext>(b => {
                services.AddSingleton(new CompletionProducer.Options() {
                    LogLevel = LogLevel.Information,
                });
                b.AddDbOperations((_, o) => {
                    o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(5);
                });
                b.AddFileBasedDbOperationLogChangeTracking(dbPath + "_changed");
                b.AddDbEntityResolver<string, DbProduct>();
                b.AddDbEntityResolver<string, DbCart>((_, options) => {
                    // Cart is always loaded together with items
                    options.QueryTransformer = carts => carts.Include(c => c.Items);
                });
            });
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> log)
        {
            app.UseWebSockets(new WebSocketOptions() {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
            });
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapFusionWebSocketServer();
                endpoints.MapControllers();
            });
        }

        public static void ConfigureLogging(IServiceCollection services)
        {
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Error);
                // logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
                // logging.AddFilter("Stl.Fusion.Operations", LogLevel.Information);
            });
        }
    }
}
