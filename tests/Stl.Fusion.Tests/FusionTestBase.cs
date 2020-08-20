using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.IO;
using Stl.Fusion.Bridge;
using Stl.Fusion.Caching;
using Stl.Fusion.Client;
using Stl.Fusion.Interception;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Fusion.UI;
using Stl.Testing;
using Stl.Testing.Internal;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;
using Message = Stl.Fusion.Bridge.Messages.Message;

namespace Stl.Fusion.Tests
{
    public class FusionTestOptions
    {
        public bool UseInMemoryDatabase { get; set; }
    }

    public class FusionTestBase : TestBase, IAsyncLifetime
    {
        public FusionTestOptions Options { get; }
        public bool IsLoggingEnabled { get; set; } = true;
        public IServiceProvider Services { get; }
        public ILogger Log { get; }
        public PathString DbPath { get; protected set; }
        public IPublisher Publisher => Services.GetRequiredService<IPublisher>();
        public IReplicator Replicator => Services.GetRequiredService<IReplicator>();
        public TestWebHost WebSocketHost => Services.GetRequiredService<TestWebHost>();

        public FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out)
        {
            Options = options ?? new FusionTestOptions();
            Services = CreateServices();
            Log = (ILogger) Services.GetService(typeof(ILogger<>).MakeGenericType(GetType()));
        }

        public virtual async Task InitializeAsync()
        {
            if (File.Exists(DbPath))
                File.Delete(DbPath);
            await using var dbContext = RentDbContext();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public virtual Task DisposeAsync()
        {
            if (Services is IDisposable d)
                d.Dispose();
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
            => DisposeAsync().Wait();

        protected virtual IServiceProvider CreateServices()
        {
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(services);
            return services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Out);

            // Logging
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.Fusion",
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

            // DbContext & related services
            var testType = GetType();
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            DbPath = appTempDir & PathEx.GetHashedName($"{testType.Name}_{testType.Namespace}.db");

            if (Options.UseInMemoryDatabase)
                services
                    .AddEntityFrameworkInMemoryDatabase()
                    .AddDbContextPool<TestDbContext>(builder => {
                        builder.UseInMemoryDatabase(DbPath);
                    }, 256);
            else
                services
                    .AddEntityFrameworkSqlite()
                    .AddDbContextPool<TestDbContext>(builder => {
                        builder.UseSqlite($"Data Source={DbPath}", sqlite => { });
                    }, 256);

            // Cache
            services.AddSingleton<SimpleCache<InterceptedInput, Result<object>>>();
            services.AddSingleton<
                ICache<InterceptedInput, Result<object>>,
                LoggingCacheWrapper<InterceptedInput, Result<object>, SimpleCache<InterceptedInput, Result<object>>>>();
            services.AddSingleton(c => new LoggingCacheWrapper<InterceptedInput, Result<object>, SimpleCache<InterceptedInput, Result<object>>>.Options() {
                LogLevel = LogLevel.Information,
            });

            // Core fusion services
            services.AddSingleton(c => new TestWebHost(c));
            services.AddFusionServerCore();
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = c.GetRequiredService<TestWebHost>().ServerUri;
                o.MessageLogLevel = LogLevel.Information;
            });
            services.AddSingleton(c => {
                var baseUri = c.GetRequiredService<TestWebHost>().ServerUri;
                var apiUri = new Uri($"{baseUri}api/");
                return new HttpClient() { BaseAddress = apiUri };
            });

            // Auto-discovered services
            services.AddDiscoveredServices(t => t.Namespace!.StartsWith(testType.Namespace!), testType.Assembly);

            // Custom live state updater
            services.AddLiveState<Unit, ServerTimeModel>(
                async (c, liveState, cancellationToken) => {
                    var client = c.GetRequiredService<IClientTimeService>();
                    var time = await client.GetTimeAsync(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel(time);
                }, (c, options) => {
                    options.InitialState = new ServerTimeModel();
                });
        }

        protected TestDbContext RentDbContext()
            => Services.GetRequiredService<DbContextPool<TestDbContext>>().Rent();

        protected Task<Channel<Message>> ConnectToPublisherAsync(CancellationToken cancellationToken = default)
        {
            var channelProvider = Services.GetRequiredService<IChannelProvider>();
            return channelProvider.CreateChannelAsync(Publisher.Id, cancellationToken);
        }

        protected virtual TestChannelPair<Message> CreateChannelPair(
            string name, bool dump = true)
            => new TestChannelPair<Message>(name, dump ? Out : null);
    }
}
