using Microsoft.EntityFrameworkCore;
using Samples.HelloCart.V2;
using Stl.Fusion.EntityFramework;
using Stl.IO;

namespace Samples.HelloCart.V3;

public class AppV3 : AppBase
{
    public AppV3()
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
            fusion.AddService<IProductService, DbProductService2>();
            fusion.AddService<ICartService, DbCartService2>();
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
            db.AddEntityResolver<string, DbProduct>();
            db.AddEntityResolver<string, DbCart>(_ => new() {
                // Cart is always loaded together with items
                QueryTransformer = carts => carts.Include(c => c.Items),
            });
        });
        ClientServices = ServerServices = services.BuildServiceProvider();
    }
}
