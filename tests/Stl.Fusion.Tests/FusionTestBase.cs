using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Stl.IO;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Npgsql;
using Stl.Fusion.EntityFramework.Redis;
using Stl.Fusion.Extensions;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Fusion.Internal;
using Stl.Fusion.Server;
using Stl.Locking;
using Stl.RegisterAttributes;
using Stl.Testing.Collections;
using Stl.Testing.Output;
using Stl.Time.Testing;
using Xunit.DependencyInjection.Logging;

namespace Stl.Fusion.Tests;

public enum FusionTestDbType
{
    Sqlite = 0,
    PostgreSql = 1,
    MariaDb = 2,
    SqlServer = 3,
    InMemory = 4,
}

public class FusionTestOptions
{
    public FusionTestDbType DbType { get; set; } = FusionTestDbType.Sqlite;
    public bool UseRedisOperationLogChangeTracking { get; set; } = !TestRunnerInfo.IsBuildAgent();
    public bool UseInMemoryKeyValueStore { get; set; }
    public bool UseInMemoryAuthService { get; set; }
    public bool UseTestClock { get; set; }
    public bool UseLogging { get; set; } = true;
}

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class FusionTestBase : TestBase, IAsyncLifetime
{
    private static readonly AsyncLock InitializeLock = new(ReentryMode.CheckedFail);

    public FusionTestOptions Options { get; }
    public bool IsLoggingEnabled { get; set; } = true;
    public FilePath SqliteDbPath { get; protected set; }
    public string PostgreSqlConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=5432;User Id=postgres;Password=postgres";
    public string MariaDbConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=3306;User=root;Password=mariadb";
    public string SqlServerConnectionString { get; protected set; } =
        "Server=localhost,1433;Database=stl_fusion_tests;MultipleActiveResultSets=True;User Id=sa;Password=SqlServer1";
    public FusionTestWebHost WebHost { get; }
    public IServiceProvider Services { get; }
    public IServiceProvider WebServices { get; }
    public IServiceProvider ClientServices { get; }
    public ILogger Log { get; }

    public FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out)
    {
        Options = options ?? new FusionTestOptions();
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        SqliteDbPath = appTempDir & FilePath.GetHashedName($"{GetType().Name}_{GetType().Namespace}.db");

        // ReSharper disable once VirtualMemberCallInConstructor
        Services = CreateServices();
        WebHost = Services.GetRequiredService<FusionTestWebHost>();
        WebServices = WebHost.Services;
        ClientServices = CreateServices(true);
        if (Options.UseLogging)
            Log = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        else
            Log = NullLogger.Instance;
    }

    public override async Task InitializeAsync()
    {
        using var __ = await InitializeLock.Lock().ConfigureAwait(false);

        for (var i = 0; i < 10 && File.Exists(SqliteDbPath); i++) {
            try {
                File.Delete(SqliteDbPath);
                break;
            }
            catch {
                await Delay(0.3);
            }
        }

        var dbContext = CreateDbContext();
        await using var _ = dbContext.ConfigureAwait(false);

        await dbContext.Database.EnsureDeletedAsync();
        try {
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch {
            // Intended - somehow it fails on GitHub build agent
        }
        Out.WriteLine("DB is recreated.");
        await Services.HostedServices().Start();
    }

    public override async Task DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable adcs)
            await adcs.DisposeAsync();
        if (ClientServices is IDisposable dcs)
            dcs.Dispose();

        try {
            await Services.HostedServices().Stop();
        }
        catch {
            // Intended
        }

        if (Services is IAsyncDisposable ads)
            await ads.DisposeAsync();
        if (Services is IDisposable ds)
            ds.Dispose();
    }

    protected bool MustSkip()
        => TestRunnerInfo.IsGitHubAction()
            && Options.DbType
                is FusionTestDbType.PostgreSql
                or FusionTestDbType.MariaDb
                or FusionTestDbType.SqlServer;

    protected IServiceProvider CreateServices(bool isClient = false)
    {
        var services = (IServiceCollection) new ServiceCollection();
        ConfigureServices(services, isClient);
        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services, bool isClient = false)
    {
        if (Options.UseTestClock)
            services.AddSingleton(new MomentClockSet(new TestClock()));
        services.AddSingleton(Out);

        // Logging
        if (Options.UseLogging)
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.Fusion",
                    "Stl.CommandR",
                    "Stl.Tests.Fusion",
                    // DbLoggerCategory.Database.Transaction.Name,
                    // DbLoggerCategory.Database.Connection.Name,
                    // DbLoggerCategory.Database.Command.Name,
                    // DbLoggerCategory.Query.Name,
                    // DbLoggerCategory.Update.Name,
                };

                bool LogFilter(string category, LogLevel level)
                    => IsLoggingEnabled &&
                        debugCategories.Any(category.StartsWith)
                        && level >= LogLevel.Debug;

                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(LogFilter);
                logging.AddDebug();
                // XUnit logging requires weird setup b/c otherwise it filters out
                // everything below LogLevel.Information
                logging.AddProvider(new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor(Out),
                    LogFilter));
            });

        // Core Fusion services
        var fusion = services.AddFusion();
        fusion.AddOperationReprocessor();
        fusion.AddFusionTime();

        // Auto-discovered services
        var testType = GetType();
        services.UseRegisterAttributeScanner()
            .WithTypeFilter(testType.Namespace!)
            .RegisterFrom(testType.Assembly);

        if (!isClient) {
            // Configuring Services and ServerServices
            services.UseRegisterAttributeScanner(ServiceScope.Services)
                .WithTypeFilter(testType.Namespace!)
                .RegisterFrom(testType.Assembly);

            // DbContext & related services
            services.AddPooledDbContextFactory<TestDbContext>(builder => {
                switch (Options.DbType) {
                case FusionTestDbType.Sqlite:
                    builder.UseSqlite($"Data Source={SqliteDbPath}");
                    break;
                case FusionTestDbType.InMemory:
                    builder.UseInMemoryDatabase(SqliteDbPath)
                        .ConfigureWarnings(warnings => {
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                        });
                    break;
                case FusionTestDbType.PostgreSql:
                    builder.UseNpgsql(PostgreSqlConnectionString, npgsql => {
                        npgsql.EnableRetryOnFailure(0);
                    });
                    builder.UseNpgsqlHintFormatter();
                    break;
                case FusionTestDbType.MariaDb:
#if NET5_0_OR_GREATER || NETCOREAPP
                    var serverVersion = ServerVersion.AutoDetect(MariaDbConnectionString);
                    builder.UseMySql(MariaDbConnectionString, serverVersion, mySql => {
#else
                    builder.UseMySql(MariaDbConnectionString, mySql => {
#endif
                        mySql.EnableRetryOnFailure(0);
                    });
                    break;
                case FusionTestDbType.SqlServer:
                    builder.UseSqlServer(SqlServerConnectionString, sqlServer => {
                        sqlServer.EnableRetryOnFailure(0);
                    });
                    break;
                default:
                    throw new NotSupportedException();
                }
#if NET5_0_OR_GREATER
                if (Options.DbType != FusionTestDbType.InMemory)
                    builder.UseValidationCheckConstraints(c => c.UseRegex(false));
#endif
                builder.EnableSensitiveDataLogging();
            }, 256);
            services.AddDbContextServices<TestDbContext>(db => {
                if (Options.UseRedisOperationLogChangeTracking)
                    db.AddRedisDb("localhost", "Fusion.Tests");
                db.AddOperations(operations => {
                    operations.ConfigureOperationLogReader(_ => new() {
                        UnconditionalCheckPeriod = TimeSpan.FromSeconds(5),
                        // Enable this if you debug multi-host invalidation
                        // MaxCommitDuration = TimeSpan.FromMinutes(5), 
                    });
                    if (Options.UseRedisOperationLogChangeTracking)
                        operations.AddRedisOperationLogChangeTracking();
                    else if (Options.DbType == FusionTestDbType.PostgreSql)
                        operations.AddNpgsqlOperationLogChangeTracking();
                    else
                        operations.AddFileBasedOperationLogChangeTracking();
                });

                if (!Options.UseInMemoryAuthService)
                    db.AddAuthentication<DbAuthSessionInfo, DbAuthUser, long>();
                if (!Options.UseInMemoryKeyValueStore)
                    db.AddKeyValueStore();
                db.AddEntityResolver<long, User>();
            });
            if (Options.UseInMemoryKeyValueStore)
                fusion.AddInMemoryKeyValueStore();
            if (Options.UseInMemoryAuthService)
                fusion.AddAuthentication().AddBackend();

            // WebHost
            var webHost = (FusionTestWebHost?) WebHost;
            if (webHost == null) {
                var webHostOptions = new FusionTestWebHostOptions();
#if NETFRAMEWORK
                var controllerTypes = testType.Assembly.GetControllerTypes(testType.Namespace).ToArray();
                webHostOptions.ControllerTypes = controllerTypes;
#endif
                webHost = new FusionTestWebHost(services, webHostOptions);
            }
            services.AddSingleton(c => webHost);
        }
        else {
            // Configuring ClientServices
            services.UseRegisterAttributeScanner(ServiceScope.ClientServices)
                .WithTypeFilter(testType.Namespace!)
                .RegisterFrom(testType.Assembly);

            // Fusion client
            var fusionClient = fusion.AddRestEaseClient();
            fusionClient.ConfigureHttpClient((_, name, options) => {
                var baseUri = WebHost.ServerUri;
                var apiUri = new Uri($"{baseUri}api/");
                var isFusionService = !(name ?? "").Contains("Tests");
                var clientBaseUri = isFusionService ? baseUri : apiUri;
                options.HttpClientActions.Add(c => c.BaseAddress = clientBaseUri);
            });
            fusionClient.ConfigureWebSocketChannel(_ => new() {
                BaseUri = WebHost.ServerUri,
                MessageLogLevel = LogLevel.Information,
            });
            fusion.AddAuthentication(fusionAuth => fusionAuth.AddRestEaseClient());

            // Custom computed state
            services.AddSingleton(c => c.StateFactory().NewComputed<ServerTimeModel2>(
                new() { InitialValue = new(default) },
                async (_, cancellationToken) => {
                    var client = c.GetRequiredService<IClientTimeService>();
                    var time = await client.GetTime(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel2(time);
                }));
        }
    }

    protected TestDbContext CreateDbContext()
        => Services.GetRequiredService<DbHub<TestDbContext>>().CreateDbContext(readWrite: true);

    protected Task<Channel<BridgeMessage>> ConnectToPublisher(CancellationToken cancellationToken = default)
    {
        var publisher = WebServices.GetRequiredService<IPublisher>();
        var channelProvider = ClientServices.GetRequiredService<IChannelProvider>();
        return channelProvider.CreateChannel(publisher.Id, cancellationToken);
    }

    protected TestChannelPair<BridgeMessage> CreateChannelPair(
        string name, bool dump = true)
        => new(name, dump ? Out : null);

    protected Task Delay(double seconds)
        => Timeouts.Clock.Delay(TimeSpan.FromSeconds(seconds));

    protected void GCCollect()
    {
        for (var i = 0; i < 3; i++) {
            GC.Collect();
            Thread.Sleep(10);
        }
    }
}
