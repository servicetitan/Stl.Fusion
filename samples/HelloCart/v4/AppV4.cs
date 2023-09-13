using Microsoft.EntityFrameworkCore;
using Samples.HelloCart.V2;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Redis;
using Stl.Fusion.Server;
using Stl.IO;
using Stl.Rpc;
using Stl.Rpc.Server;

namespace Samples.HelloCart.V4;

public class AppV4 : AppBase
{
    public WebApplication App { get; protected set; }

    public AppV4()
    {
        var uri = "http://localhost:7005";

        // Create server
        App = CreateWebApp(uri);
        ServerServices = App.Services;

        // Create client
        ClientServices = BuildClientServices(uri);
    }

    protected WebApplication CreateWebApp(string baseUri)
    {
        var builder = WebApplication.CreateBuilder();

        // Configure services
        var services = builder.Services;
        services.AddFusion(RpcServiceMode.Server, fusion => {
            fusion.AddWebServer();
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
                operations.AddRedisOperationLogChangeTracking();
            });
            db.AddRedisDb("localhost", "Fusion.Samples.HelloCart");
            db.AddEntityResolver<string, DbProduct>();
            db.AddEntityResolver<string, DbCart>(_ => new() {
                // Cart is always loaded together with items
                QueryTransformer = carts => carts.Include(c => c.Items),
            });
        });

        // Configure WebApplication
        var app = builder.Build();
        app.Urls.Add(baseUri);
        app.UseFusionSession();
        app.UseWebSockets(new WebSocketOptions() {
            KeepAliveInterval = TimeSpan.FromSeconds(30),
        });
        app.MapRpcWebSocketServer();
        return app;
    }

    protected IServiceProvider BuildClientServices(string baseUri)
    {
        var services = new ServiceCollection();
        ConfigureLogging(services);
        services.AddFusion(fusion => {
            fusion.Rpc.AddWebSocketClient(baseUri);
            fusion.AddClient<IProductService>();
            fusion.AddClient<ICartService>();
        });
        return services.BuildServiceProvider();
    }

    protected void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Error);
            // logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            // logging.AddFilter("Stl.Fusion.Operations", LogLevel.Information);
        });
    }

    public override async Task InitializeAsync(IServiceProvider services)
    {
        // Let's re-create the database first
        await using var dbContext = ServerServices.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await base.InitializeAsync(services);

        await App.StartAsync();
        await Task.Delay(100);
    }

    public override async ValueTask DisposeAsync()
    {
        // Let's stop the client first
        if (ClientServices is IAsyncDisposable csd)
            await csd.DisposeAsync();

        await App.StopAsync();
        await App.DisposeAsync();
    }
}
