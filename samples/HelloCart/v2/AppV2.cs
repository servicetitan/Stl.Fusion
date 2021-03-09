using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;
using Stl.IO;

namespace Samples.HelloCart.V2
{
    public class AppV2 : AppBase
    {
        public AppV2()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Error);
                // logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
                // logging.AddFilter("Stl.Fusion.Operations", LogLevel.Information);
            });

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
                b.AddDbOperations((_, o) => {
                    o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(5);
                });
                b.AddFileBasedDbOperationLogChangeTracking(dbPath + "_changed");
            });
            ClientServices = HostServices = services.BuildServiceProvider();
        }

        public override async Task InitializeAsync()
        {
            // Let's re-create the database first
            await using var dbContext = HostServices.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            await base.InitializeAsync();
        }
    }
}
