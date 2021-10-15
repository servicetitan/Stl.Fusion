using Microsoft.EntityFrameworkCore;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;
using Stl.IO;

namespace Samples.HelloCart.V2;

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
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        var dbPath = appTempDir & "HelloCart_v01.db";
        services.AddDbContextFactory<AppDbContext>(dbContext => {
            dbContext.UseSqlite($"Data Source={dbPath}");
            dbContext.EnableSensitiveDataLogging();
        });
        services.AddDbContextServices<AppDbContext>(dbContext => {
            dbContext.AddOperations((_, o) => {
                o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(5);
            });
            dbContext.AddFileBasedOperationLogChangeTracking(dbPath + "_changed");
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
