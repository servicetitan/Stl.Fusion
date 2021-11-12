using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Samples.HelloCart.V2;
using Stl.Fusion.Client;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Redis;
using Stl.Fusion.Server;
using Stl.IO;

namespace Samples.HelloCart.V4;

public class AppV4 : AppBase
{
    public IHost Host { get; protected set; }

    public AppV4()
    {
        var baseUri = new Uri("http://localhost:7005");
        Host = BuildHost(baseUri);
        HostServices = Host.Services;
        ClientServices = BuildClientServices(baseUri);
    }

    protected IHost BuildHost(Uri baseUri)
        => new HostBuilder()
            .ConfigureHostConfiguration(cfg => {
                // Looks like there is no better way to set _default_ URL
                cfg.Sources.Insert(0, new MemoryConfigurationSource() {
                    InitialData = new Dictionary<string, string>() {
                        {WebHostDefaults.ServerUrlsKey, baseUri.ToString()},
                    }
                });
            })
            .ConfigureWebHostDefaults(webHost => webHost
                .ConfigureServices(services => {
                    ConfigureLogging(services);
                    services.AddFusion(fusion => {
                        fusion.AddComputeService<IProductService, DbProductService>();
                        fusion.AddComputeService<ICartService, DbCartService>();
                        fusion.AddWebServer();
                    });

                    // Add AppDbContext & related services
                    var appTempDir = FilePath.GetApplicationTempDirectory("", true);
                    var dbPath = appTempDir & "HelloCart_v01.db";
                    services.AddDbContextFactory<AppDbContext>(dbContext => {
                        dbContext.UseSqlite($"Data Source={dbPath}");
                        dbContext.EnableSensitiveDataLogging();
                    });
                    services.AddDbContextServices<AppDbContext>(dbContext => {
                        dbContext.AddOperations((_, o) => { o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(5); });
                        // dbContext.AddFileBasedOperationLogChangeTracking(dbPath + "_changed");
                        dbContext.AddRedisDb("localhost", "Fusion.Samples.HelloCart");
                        dbContext.AddRedisOperationLogChangeTracking();
                        dbContext.AddEntityResolver<string, DbProduct>();
                        dbContext.AddEntityResolver<string, DbCart>((_, options) => {
                            // Cart is always loaded together with items
                            options.QueryTransformer = carts => carts.Include(c => c.Items);
                        });
                    });
                })
                .Configure(app => {
                    app.UseWebSockets(new WebSocketOptions() {
                        KeepAliveInterval = TimeSpan.FromSeconds(30),
                    });
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapFusionWebSocketServer();
                        endpoints.MapControllers();
                    });
                })
            )
            .Build();

    protected IServiceProvider BuildClientServices(Uri baseUri)
    {
        var services = new ServiceCollection();
        ConfigureLogging(services);
        services.AddFusion(fusion => {
            fusion.AddRestEaseClient(client => {
                client.ConfigureHttpClientFactory((c, name, options) => {
                    var apiBaseUri = new Uri($"{baseUri}api/");
                    options.HttpClientActions.Add(httpClient => httpClient.BaseAddress = apiBaseUri);
                });
                client.ConfigureWebSocketChannel((c, options) => { options.BaseUri = baseUri; });
                client.AddReplicaService<IProductService, IProductClientDef>();
                client.AddReplicaService<ICartService, ICartClientDef>();
            });
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

    public override async Task InitializeAsync()
    {
        // Let's re-create the database first
        await using var dbContext = HostServices.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await base.InitializeAsync();
        await Host.StartAsync();
        await Task.Delay(100);
    }

    public override async ValueTask DisposeAsync()
    {
        // Let's stop the client first
        if (ClientServices is IAsyncDisposable csd)
            await csd.DisposeAsync();
        await Host.StopAsync();
        Host.Dispose();
    }
}
