using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.IO;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.Fusion.Model;
using Stl.Tests.Fusion.Services;
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

        public WebSocketClient NewWebSocketClient() 
            => Container.Resolve<WebSocketClient>();

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
                var debugCategories = new HashSet<string> {
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
                logging.SetMinimumLevel(LogLevel.Information);
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

            services.AddSingleton<ITestDbContextPool, TestDbContextPool>();

            // Core fusion services
            services.AddFusion();

            // Computed providers
            services.AddComputedProvider<ISimplestProvider, SimplestProvider>();
            services.AddComputedProvider<ITimeProvider, TimeProvider>();
            services.AddComputedProvider<IUserProvider, UserProvider>();

            // Regular services
            services.AddScoped<UserProvider, UserProvider>();

            // Bridge
            services.AddSingleton(c => new TestWebServer(c.GetRequiredService<IPublisher>()));
            services.AddFusionWebSocketClient((c, o) => {
                o.BaseUri = c.GetRequiredService<TestWebServer>().BaseUri;
            });
        }

        public virtual void ConfigureServices(ContainerBuilder builder)
        { }

        public virtual TestChannelPair<Message> CreateChannelPair(
            string name, bool dump = true) 
            => new TestChannelPair<Message>(name, dump ? Out : null);
    }
}
