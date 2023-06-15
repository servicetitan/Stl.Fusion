using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Stl.IO;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client.Cache;
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
using Stl.Testing.Output;
using Stl.Tests;
using Stl.Tests.Rpc;
using Stl.Time.Testing;
using Xunit.DependencyInjection.Logging;
using User = Stl.Fusion.Tests.Model.User;

namespace Stl.Fusion.Tests;

public enum FusionTestDbType
{
    Sqlite = 0,
    PostgreSql = 1,
    MariaDb = 2,
    SqlServer = 3,
    InMemory = 4,
}

public class FusionTestOptions : RpcTestOptions
{
    public FusionTestDbType DbType { get; set; } = FusionTestDbType.Sqlite;
    public bool UseRedisOperationLogChangeTracking { get; set; } = !TestRunnerInfo.IsBuildAgent();
    public bool UseInMemoryKeyValueStore { get; set; }
    public bool UseInMemoryAuthService { get; set; }
    public bool UseReplicaCache { get; set; }
}

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public abstract class FusionTestBase : RpcTestBase
{
    private static readonly ReentrantAsyncLock InitializeLock = new(LockReentryMode.CheckedFail);
    protected static readonly ConcurrentDictionary<Symbol, string> ReplicaCache = new();

    public new FusionTestOptions Options { get; }
    public FilePath SqliteDbPath { get; protected set; }
    public string PostgreSqlConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=5432;User Id=postgres;Password=postgres";
    public string MariaDbConnectionString { get; protected set; } =
        "Server=localhost;Database=stl_fusion_tests;Port=3306;User=root;Password=mariadb";
    public string SqlServerConnectionString { get; protected set; } =
        "Server=localhost,1433;Database=stl_fusion_tests;MultipleActiveResultSets=true;TrustServerCertificate=true;User Id=sa;Password=SqlServer1";

    protected FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options ??= new())
    {
        Options = options;
        var appTempDir = FilePath.GetApplicationTempDirectory("", true);
        SqliteDbPath = appTempDir & FilePath.GetHashedName($"{GetType().Name}_{GetType().Namespace}.db");
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

    protected virtual bool MustSkip()
        => TestRunnerInfo.IsGitHubAction()
            && Options.DbType
                is FusionTestDbType.PostgreSql
                or FusionTestDbType.MariaDb
                or FusionTestDbType.SqlServer;

    protected override void ConfigureTestServices(IServiceCollection services, bool isClient)
    {
        var fusion = services.AddFusion();
        if (!isClient) {
            fusion = fusion.WithServiceMode(RpcServiceMode.Server, true);
            fusion.AddService<ITimeService, TimeService>();
            fusion.Rpc.Service<ITimeServer>().HasServer<ITimeService>().HasName(nameof(ITimeService));
            fusion.AddService<IUserService, UserService>();
            fusion.AddService<IScreenshotService, ScreenshotService>();
            fusion.AddService<IEdgeCaseService, EdgeCaseService>();
            fusion.AddService<IKeyValueService<string>, KeyValueService<string>>();
        } else {
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
            var fusionServer = fusion.AddWebServer();
#if !NETFRAMEWORK
            fusionServer.AddAuthentication();
#endif
            if (Options.UseInMemoryAuthService)
                fusion.AddInMemoryAuthService();
            else
                fusion.AddDbAuthService<TestDbContext, DbAuthSessionInfo, DbAuthUser, long>();
            if (Options.UseInMemoryKeyValueStore)
                fusion.AddInMemoryKeyValueStore();
            else
                fusion.AddDbKeyValueStore<TestDbContext>();

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

                db.AddEntityResolver<long, User>();
            });
        }
        else {
            // Custom replica cache
            services.AddSingleton(_ => new InMemoryComputedCache.Options() {
                IsEnabled = Options.UseReplicaCache,
                Cache = ReplicaCache,
            });
            services.AddSingleton<ClientComputedCache, InMemoryComputedCache>();

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
