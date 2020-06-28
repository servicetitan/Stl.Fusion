using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.IO;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.UI;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.Fusion.Model;
using Stl.Tests.Fusion.Services;
using Stl.Tests.Fusion.UIModels;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;
using Message = Stl.Fusion.Bridge.Messages.Message;

namespace Stl.Tests.Fusion
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
        public ILifetimeScope Container { get; }
        public ILogger Log { get; }
        public TestDbContext DbContext => Container.Resolve<TestDbContext>();
        public IPublisher Publisher => Container.Resolve<IPublisher>();
        public IReplicator Replicator => Container.Resolve<IReplicator>();
        public TestWebServer WebSocketServer => Container.Resolve<TestWebServer>();

        public FusionTestBase(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out)
        {
            Options = options ?? new FusionTestOptions();
            Services = CreateServices();
            Container = Services.GetRequiredService<ILifetimeScope>();
            Log = (ILogger) Container.Resolve(typeof(ILogger<>).MakeGenericType(GetType()));
        }

        public virtual Task InitializeAsync() 
            => DbContext.Database.EnsureCreatedAsync();
        public virtual Task DisposeAsync() 
            => Task.CompletedTask.ContinueWith(_ => Container?.Dispose()); 

        protected virtual IServiceProvider CreateServices()
        {
            // IServiceCollection-based services
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(services);

            // Native Autofac services
            var builder = new AutofacServiceProviderFactory().CreateBuilder(services);
            ConfigureServices(builder);

            var container = builder.Build();
            return new AutofacServiceProvider(container);
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
            services.AddSingleton(c => new TestWebServer(c));
            services.AddFusionServerCore();
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = c.GetRequiredService<TestWebServer>().BaseUri;
                o.MessageLogLevel = LogLevel.Information;
            });


            // Computed providers
            services.AddComputedService<ISimplestProvider, SimplestProvider>();
            services.AddComputedService<ITimeService, TimeService>();
            services.AddComputedService<IUserService, UserService>();
            services.AddComputedService<IScreenshotService, ScreenshotService>();

            // Regular services
            services.AddScoped<UserService, UserService>();

            // Replica services
            services.AddReplicaService<ITimeClient>(null, (c, httpClient) => {
                var baseUri = c.GetRequiredService<TestWebServer>().BaseUri;
                httpClient.BaseAddress = new Uri($"{baseUri}api/time");
            });

            // UI Models
            services.AddLive<ServerTimeModel1>(
                async (c, prev, cancellationToken) => {
                    var client = c.GetRequiredService<ITimeClient>();
                    var time = await client.GetTimeAsync(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel1(time);
                }, (c, options) => {
                    options.Default = new ServerTimeModel1();
                });
            services.AddLive<ServerTimeModel2>(
                async (c, prev, cancellationToken) => {
                    var client = c.GetRequiredService<ITimeClient>();
                    var cTime = await client.GetComputedTimeAsync(cancellationToken).ConfigureAwait(false);
                    return new ServerTimeModel2(cTime.Value);
                }, (c, options) => {
                    options.Default = new ServerTimeModel2();
                });
        }

        protected Task<Channel<Message>> ConnectToPublisherAsync(CancellationToken cancellationToken = default)
        {
            var channelProvider = Container.Resolve<IChannelProvider>();
            return channelProvider.CreateChannelAsync(Publisher.Id, cancellationToken);
        }

        protected virtual void ConfigureServices(ContainerBuilder builder)
        { }

        protected virtual TestChannelPair<Message> CreateChannelPair(
            string name, bool dump = true) 
            => new TestChannelPair<Message>(name, dump ? Out : null);
    }
}
