using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.Async;
using Stl.DependencyInjection;
using Stl.IO;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Fusion.Client;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Npgsql;
using Stl.Fusion.Extensions;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Fusion.Internal;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Fusion.Tests
{
    public enum FusionTestDbType
    {
        Sqlite = 0,
        InMemory = 1,
        PostgreSql = 2,
    }

    public class FusionTestOptions
    {
        public FusionTestDbType DbType { get; set; } = FusionTestDbType.Sqlite;
        public bool UseInMemoryAuthService { get; set; }
        public bool UseTestClock { get; set; }
        public bool UseLogging { get; set; } = true;
    }

    public class FusionTestBase : TestBase, IAsyncLifetime
    {
        public FusionTestOptions Options { get; }
        public bool IsLoggingEnabled { get; set; } = true;
        public PathString SqliteDbPath { get; protected set; }
        public string PostgreSqlConnectionString { get; protected set; } =
            "Server=localhost;Database=stl_fusion_tests;Port=5432;User Id=postgres;Password=Fusion.0.to.1";
        public FusionTestWebHost WebHost { get; }
        public IServiceProvider Services { get; }
        public IServiceProvider WebServices { get; }
        public IServiceProvider ClientServices { get; }
        public ILogger Log { get; }

        public FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out)
        {
            Options = options ?? new FusionTestOptions();
            // ReSharper disable once VirtualMemberCallInConstructor
            Services = CreateServices();
            WebHost = Services.GetRequiredService<FusionTestWebHost>();
            WebServices = WebHost.Services;
            ClientServices = CreateServices(true);
            Log = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        }

        public override async Task InitializeAsync()
        {
            for (var i = 0; i < 10 && File.Exists(SqliteDbPath); i++) {
                try {
                    File.Delete(SqliteDbPath);
                    break;
                }
                catch {
                    await Delay(0.3);
                }
            }
            await using var dbContext = CreateDbContext();
            if (Options.DbType != FusionTestDbType.Sqlite)
                await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            await Services.HostedServices().Start();
        }

        public override async Task DisposeAsync()
        {
            if (ClientServices is IDisposable dcs)
                dcs.Dispose();
            try {
                await Services.HostedServices().Stop();
            }
            finally {
                if (Services is IDisposable ds)
                    ds.Dispose();
            }
        }

        protected IServiceProvider CreateServices(bool isClient = false)
        {
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(services, isClient);
            return services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices(IServiceCollection services, bool isClient = false)
        {
            if (Options.UseTestClock)
                services.AddSingleton<IMomentClock, TestClock>();
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
            fusion.AddLiveClock();

            // Auto-discovered services
            var testType = GetType();
            services.UseAttributeScanner()
                .WithTypeFilter(testType.Namespace!)
                .AddServicesFrom(testType.Assembly);

            if (!isClient) {
                // Configuring Services and ServerServices
                services.UseAttributeScanner(ServiceScope.Services)
                    .WithTypeFilter(testType.Namespace!)
                    .AddServicesFrom(testType.Assembly);

                // DbContext & related services
                var appTempDir = PathEx.GetApplicationTempDirectory("", true);
                SqliteDbPath = appTempDir & PathEx.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
                services.AddPooledDbContextFactory<TestDbContext>(builder => {
                    switch (Options.DbType) {
                    case FusionTestDbType.Sqlite:
                        builder.UseSqlite($"Data Source={SqliteDbPath}");
                        break;
                    case FusionTestDbType.InMemory:
                        builder.UseInMemoryDatabase(SqliteDbPath)
                            .ConfigureWarnings(w => {
                                w.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                            });
                        break;
                    case FusionTestDbType.PostgreSql:
                        builder.UseNpgsql(PostgreSqlConnectionString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    builder.EnableSensitiveDataLogging();
                }, 256);
                services.AddDbContextServices<TestDbContext>(b => {
                    b.AddDbEntityResolver<long, User>();
                    b.AddDbOperations((_, o) => {
                        o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(5);
                        // Enable this if you debug multi-host invalidation
                        // o.MaxCommitDuration = TimeSpan.FromMinutes(5);
                    });
                    b.AddKeyValueStore();
                    if (Options.DbType == FusionTestDbType.PostgreSql)
                        b.AddNpgsqlDbOperationLogChangeTracking();
                    else
                        b.AddFileBasedDbOperationLogChangeTracking();
                    if (!Options.UseInMemoryAuthService)
                        b.AddDbAuthentication();
                });
                if (Options.UseInMemoryAuthService)
                    fusion.AddAuthentication().AddServerSideAuthService();

                // WebHost
                var webHost = (FusionTestWebHost?) WebHost;
                services.AddSingleton(c => webHost ?? new FusionTestWebHost(services));
            }
            else {
                // Configuring ClientServices
                services.UseAttributeScanner(ServiceScope.ClientServices)
                    .WithTypeFilter(testType.Namespace!)
                    .AddServicesFrom(testType.Assembly);

                // Fusion client
                var fusionClient = fusion.AddRestEaseClient(
                    (c, options) => {
                        options.BaseUri = WebHost.ServerUri;
                        options.MessageLogLevel = LogLevel.Information;
                    }).ConfigureHttpClientFactory(
                    (c, name, options) => {
                        var baseUri = WebHost.ServerUri;
                        var apiUri = new Uri($"{baseUri}api/");
                        var isFusionService = !(name ?? "").Contains("Tests");
                        var clientBaseUri = isFusionService ? baseUri : apiUri;
                        options.HttpClientActions.Add(c => c.BaseAddress = clientBaseUri);
                    });
                fusion.AddAuthentication(fusionAuth => fusionAuth.AddRestEaseClient());

                // Custom live state
                fusion.AddState(c => c.StateFactory().NewLive<ServerTimeModel2>(
                    async (state, cancellationToken) => {
                        var client = c.GetRequiredService<IClientTimeService>();
                        var time = await client.GetTime(cancellationToken).ConfigureAwait(false);
                        return new ServerTimeModel2(time);
                    }));
            }
        }

        protected TestDbContext CreateDbContext()
            => Services.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();

        protected Task<Channel<BridgeMessage>> ConnectToPublisher(CancellationToken cancellationToken = default)
        {
            var publisher = WebServices.GetRequiredService<IPublisher>();
            var channelProvider = ClientServices.GetRequiredService<IChannelProvider>();
            return channelProvider.CreateChannel(publisher.Id, cancellationToken);
        }

        protected virtual TestChannelPair<BridgeMessage> CreateChannelPair(
            string name, bool dump = true)
            => new(name, dump ? Out : null);

        protected virtual Task Delay(double seconds)
            => Timeouts.Clock.Delay(TimeSpan.FromSeconds(seconds));

        protected void GCCollect()
        {
            for (var i = 0; i < 3; i++) {
                GC.Collect();
                Thread.Sleep(10);
            }
        }
    }
}
