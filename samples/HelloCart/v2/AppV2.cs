using Microsoft.EntityFrameworkCore;
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
            fusion.AddService<IProductService, DbProductService>();
            fusion.AddService<ICartService, DbCartService>();
        });

        // Add AppDbContext & related services
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        var dbPath = appTempDir & "HelloCart_v01.db";
        services.AddTransientDbContextFactory<AppDbContext>(db => {
            db.UseSqlite($"Data Source={dbPath}");
            db.EnableSensitiveDataLogging();
        });
        services.AddDbContextServices<AppDbContext>(db => {
            db.AddOperations(operations => {
                operations.AddFileBasedOperationLogChangeTracking();
            });
        });
        ClientServices = ServerServices = services.BuildServiceProvider();
    }

    public override async Task InitializeAsync(IServiceProvider services)
    {
        // Let's re-create the database first
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await base.InitializeAsync(services);
    }
}
