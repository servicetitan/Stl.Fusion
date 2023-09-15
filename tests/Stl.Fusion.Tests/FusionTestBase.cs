using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Stl.IO;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client.Caching;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Npgsql;
using Stl.Fusion.EntityFramework.Redis;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;
using Stl.Fusion.Tests.Extensions;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Locking;
using Stl.Rpc;
using Stl.Testing.Collections;
using Stl.Tests;
using User = Stl.Fusion.Tests.Model.User;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public abstract class FusionTestBase : RpcTestBase
{
    private static readonly ReentrantAsyncLock InitializeLock = new(LockReentryMode.CheckedFail);
    protected static readonly ConcurrentDictionary<Symbol, string> ClientComputedCacheStore = new();

    public FusionTestDbType DbType { get; init; } = TestRunnerInfo.IsBuildAgent()
        ? FusionTestDbType.InMemory
        : FusionTestDbType.Sqlite;
    public bool UseRedisOperationLogChangeTracking { get; init; } = !TestRunnerInfo.IsBuildAgent();
    public bool UseInMemoryKeyValueStore { get; init; }
    public bool UseInMemoryAuthService { get; init; }
    public bool UseClientComputedCache { get; init; }
    public LogLevel RpcCallLogLevel { get; init; } = LogLevel.None;

    public FilePath SqliteDbPath { get; protected set; }
    public string PostgreSqlConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=5432;User Id=postgres;Password=postgres;Enlist=false;Minimum Pool Size=5;Maximum Pool Size=20;Connection Idle Lifetime=30";
    public string MariaDbConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=3306;User=root;Password=mariadb";
    public string SqlServerConnectionString { get; protected set; } =
        "Server=localhost,1433;Database=stl_fusion_tests;MultipleActiveResultSets=true;TrustServerCertificate=true;User Id=sa;Password=SqlServer1";

    protected FusionTestBase(ITestOutputHelper @out) : base(@out)
    {
        var appTempDir = TestRunnerInfo.IsGitHubAction()
            ? new FilePath(Environment.GetEnvironmentVariable("RUNNER_TEMP"))
            : FilePath.GetApplicationTempDirectory("", true);
        SqliteDbPath = appTempDir & FilePath.GetHashedName($"{GetType().Name}_{GetType().Namespace}.db");
    }

    public override async Task InitializeAsync()
    {
        if (!DbType.IsAvailable())
            return;

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

    protected virtual bool MustSkip()
        => !DbType.IsAvailable();

    protected override void ConfigureTestServices(IServiceCollection services, bool isClient)
    {
        var fusion = services.AddFusion();
        var rpc = fusion.Rpc;
        if (!isClient) {
            fusion.AddService<ITimeService, TimeService>();
            rpc.Service<ITimeService>().Remove();
            rpc.Service<ITimeServer>().HasServer<ITimeService>().HasName(nameof(ITimeService));
            fusion.AddService<IUserService, UserService>();
            fusion.AddService<IScreenshotService, ScreenshotService>();
            fusion.AddService<IEdgeCaseService, EdgeCaseService>();
            fusion.AddService<IKeyValueService<string>, KeyValueService<string>>();
        } else {
            services.AddSingleton<RpcPeerFactory>(_ => (hub, peerRef)
                => peerRef.IsServer
                    ? new RpcServerPeer(hub, peerRef) { CallLogLevel = RpcCallLogLevel }
                    : new RpcClientPeer(hub, peerRef) { CallLogLevel = RpcCallLogLevel });
            if (UseClientComputedCache)
                fusion.AddSharedClientComputedCache<
                    InMemoryClientComputedCache,
                    FlushingClientComputedCache.Options>(_ => new() {
                    Version = CpuTimestamp.Now.ToString(),
                });
            fusion.AddClient<ITimeService>();
            fusion.AddClient<IUserService>();
            fusion.AddClient<IScreenshotService>();
            fusion.AddClient<IEdgeCaseService>();
            fusion.AddClient<IKeyValueService<string>>();
        }
        services.AddSingleton<UserService>();
        services.AddSingleton<IComputedState<ServerTimeModel1>, ServerTimeModel1State>();
        services.AddSingleton<IComputedState<KeyValueModel<string>>, StringKeyValueModelState>();
        fusion.AddService<ISimplestProvider, SimplestProvider>(ServiceLifetime.Scoped);
        fusion.AddService<NestedOperationLoggerTester>();
    }

    protected override void ConfigureServices(IServiceCollection services, bool isClient)
    {
        base.ConfigureServices(services, isClient);

        // Core Fusion services
        var fusion = services.AddFusion();
        fusion.AddOperationReprocessor();
        fusion.AddFusionTime();

        if (!isClient) {
            fusion = fusion.WithServiceMode(RpcServiceMode.Server, true);
            var fusionServer = fusion.AddWebServer();
#if !NETFRAMEWORK
            fusionServer.AddMvc().AddControllers();
#endif
            if (UseInMemoryAuthService)
                fusion.AddInMemoryAuthService();
            else
                fusion.AddDbAuthService<TestDbContext, DbAuthSessionInfo, DbAuthUser, long>();
            if (UseInMemoryKeyValueStore)
                fusion.AddInMemoryKeyValueStore();
            else
                fusion.AddDbKeyValueStore<TestDbContext>();

            // DbContext & related services
            services.AddTransientDbContextFactory<TestDbContext>(db => {
                switch (DbType) {
                case FusionTestDbType.Sqlite:
                    db.UseSqlite($"Data Source={SqliteDbPath}");
                    break;
                case FusionTestDbType.InMemory:
                    db.UseInMemoryDatabase(SqliteDbPath)
                        .ConfigureWarnings(warnings => {
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                        });
                    break;
                case FusionTestDbType.PostgreSql:
                    db.UseNpgsql(PostgreSqlConnectionString, npgsql => {
                        npgsql.EnableRetryOnFailure(0);
                    });
                    db.UseNpgsqlHintFormatter();
                    break;
                case FusionTestDbType.MariaDb:
#if NET5_0_OR_GREATER || NETCOREAPP
                    var serverVersion = ServerVersion.AutoDetect(MariaDbConnectionString);
                    db.UseMySql(MariaDbConnectionString, serverVersion, mySql => {
#else
                    db.UseMySql(MariaDbConnectionString, mySql => {
#endif
                        mySql.EnableRetryOnFailure(0);
                    });
                    break;
                case FusionTestDbType.SqlServer:
                    db.UseSqlServer(SqlServerConnectionString, sqlServer => {
                        sqlServer.EnableRetryOnFailure(0);
                    });
                    break;
                default:
                    throw new NotSupportedException();
                }
                db.EnableSensitiveDataLogging();
            });
            services.AddDbContextServices<TestDbContext>(db => {
                if (UseRedisOperationLogChangeTracking)
                    db.AddRedisDb("localhost", "Fusion.Tests");
                db.AddOperations(operations => {
                    operations.ConfigureOperationLogReader(_ => new() {
                        // Enable this if you debug multi-host invalidation
                        // MaxCommitDuration = TimeSpan.FromMinutes(5),
                    });
                    if (UseRedisOperationLogChangeTracking)
                        operations.AddRedisOperationLogChangeTracking();
                    else if (DbType == FusionTestDbType.PostgreSql)
                        operations.AddNpgsqlOperationLogChangeTracking();
                    else
                        operations.AddFileBasedOperationLogChangeTracking();
                });

                db.AddEntityResolver<long, User>();
            });
        }
        else {
            fusion.AddAuthClient();

            // Custom computed state
            services.AddSingleton(c => c.StateFactory().NewComputed<ServerTimeModel2>(
                new() { InitialValue = new(default) },
                async (_, cancellationToken) => {
                    var client = c.GetRequiredService<ITimeService>();
                    var time = await client.GetTime(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel2(time);
                }));
        }
    }

    protected TestDbContext CreateDbContext()
        => Services.GetRequiredService<DbHub<TestDbContext>>().CreateDbContext(readWrite: true);
}
