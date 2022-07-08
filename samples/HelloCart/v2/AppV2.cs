using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Operations;
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
        services.AddDbContextFactory<AppDbContext>(db => {
            db.UseSqlite($"Data Source={dbPath}");
            db.EnableSensitiveDataLogging();
        });
        services.AddDbContextServices<AppDbContext>(db => {
            db.AddOperations(operations => {
                operations.ConfigureOperationLogReader(_ => new() {
                    UnconditionalCheckPeriod = TimeSpan.FromSeconds(5),
                });
                operations.AddFileBasedOperationLogChangeTracking();
            });
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
