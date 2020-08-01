using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.IO;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Fusion.UI;
using Stl.Testing;
using Stl.Testing.Internal;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;
using Message = Stl.Fusion.Bridge.Messages.Message;

namespace Stl.Fusion.Tests
{
    public class FusionTestOptions
    {
        public bool UseInMemoryDatabase { get; set; }
    }

    public class FusionTestBase : TestBase
    {
        public FusionTestOptions Options { get; }
        public bool IsLoggingEnabled { get; set; } = true;
        public IServiceProvider Services { get; }
        public ILogger Log { get; }
        public TestDbContext DbContext => Services.GetRequiredService<TestDbContext>();
        public IPublisher Publisher => Services.GetRequiredService<IPublisher>();
        public IReplicator Replicator => Services.GetRequiredService<IReplicator>();
        public TestWebHost WebSocketHost => Services.GetRequiredService<TestWebHost>();

        public FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out)
        {
            Options = options ?? new FusionTestOptions();
            Services = CreateServices();
            Log = (ILogger) Services.GetService(typeof(ILogger<>).MakeGenericType(GetType()));
        }

        public virtual Task InitializeAsync()
            => DbContext.Database.EnsureCreatedAsync();
        public virtual Task DisposeAsync()
            => Task.CompletedTask.ContinueWith(_ => (Services as IDisposable)?.Dispose());

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
            var dbPath = appTempDir & PathEx.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            if (Options.UseInMemoryDatabase)
                services
                    .AddEntityFrameworkInMemoryDatabase()
                    .AddDbContextPool<TestDbContext>(builder => {
                        builder.UseInMemoryDatabase(dbPath);
                    });
            else
                services
                    .AddEntityFrameworkSqlite()
                    .AddDbContextPool<TestDbContext>(builder => {
                        builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
                    });
            services.AddSingleton<TestDbContextPool>();

            // Core fusion services
            services.AddSingleton(c => new TestWebHost(c));
            services.AddFusionServerCore();
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = c.GetRequiredService<TestWebHost>().ServerUri;
                o.MessageLogLevel = LogLevel.Information;
            });
            services.AddHttpClient<HttpClient>((c, httpClient) => {
                var baseUri = c.GetRequiredService<TestWebHost>().ServerUri;
                var apiUri = new Uri($"{baseUri}api/");
                httpClient.BaseAddress = apiUri;
            });
            services.AddSingleton(c => {
                var baseUri = c.GetRequiredService<TestWebHost>().ServerUri;
                var apiUri = new Uri($"{baseUri}api/");
                return new HttpClient() { BaseAddress = apiUri };
            });
            services.AddServices(t => t.Namespace!.StartsWith(testType.Namespace!), testType.Assembly);

            // Custom live state updater
            services.AddLiveState<ServerTimeModel2>(
                async (c, prev, cancellationToken) => {
                    var client = c.GetRequiredService<ITimeServiceClient>();
                    var cTime = await client.GetComputedTimeAsync(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel2(cTime.Value);
                }, (c, options) => {
                    options.InitialState = new ServerTimeModel2();
                });
        }

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
