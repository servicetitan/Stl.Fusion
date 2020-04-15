using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using EnumsNET;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Concurrency;
using Stl.IO;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.Purifier.Model;
using Stl.Tests.Purifier.Services;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.Purifier
{
    public class PurifierTestBase : TestBase
    {
        public IServiceProvider Services { get; }
        public ILifetimeScope Container { get; }
        public TestDbContext DbContext => Container.Resolve<TestDbContext>();

        public PurifierTestBase(ITestOutputHelper @out) : base(@out)
        {
            Services = CreateServices();
            Container = Services.GetRequiredService<ILifetimeScope>();
        }

        public virtual Task InitializeAsync() 
            => DbContext.Database.EnsureCreatedAsync();
        public virtual Task DisposeAsync() 
            => Task.CompletedTask;

        protected virtual IServiceProvider CreateServices()
        {
            // IServiceCollection-based services
            var services = (IServiceCollection) new ServiceCollection();
            ConfigureServices(ref services);

            // Native Autofac services
            var builder = new AutofacServiceProviderFactory().CreateBuilder(services);
            ConfigureServices(ref builder);

            var container = builder.Build();
            return new AutofacServiceProvider(container);
        }

        protected virtual void ConfigureServices(ref IServiceCollection services)
        {
            var testOutputLoggerProvider = new XunitTestOutputLoggerProvider(
                new SimpleTestOutputHelperAccessor(Out));

            // Logging
            services.AddLogging(logging => {
                logging.AddDebug();
                logging.AddProvider(testOutputLoggerProvider);
            });

            // DBContext
            var testType = GetType();
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            var dbPath = appTempDir & PathEx.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            services
                .AddEntityFrameworkSqlite()
                .AddDbContext<TestDbContext>(builder => {
                    builder.UseSqlite(
                        $"Data Source={dbPath}",
                        sqlite => { });
                    builder.UseLoggerFactory(LoggerFactory.Create(logger => {
                        var categories = new HashSet<string> {
                            DbLoggerCategory.Database.Transaction.Name,
                            DbLoggerCategory.Database.Connection.Name,
                            // DbLoggerCategory.Database.Command.Name,
                            DbLoggerCategory.Query.Name,
                            DbLoggerCategory.Update.Name,
                        };
                        logger.AddFilter((category, level) =>
                            categories.Contains(category)
                            && level >= LogLevel.Debug);
                        logger.AddDebug();
                        logger.AddProvider(testOutputLoggerProvider);
                    }));
                });
        }

        protected virtual void ConfigureServices(ref ContainerBuilder builder)
        {
            // Interceptors
            builder.Register(c => new ConcurrentIdGenerator<long>(i => {
                var id = i * 10000;
                return () => ++id;
            }));
            builder.RegisterType<ComputedRegistry<(IFunction, InterceptedInput)>>()
                .As<IComputedRegistry<(IFunction, InterceptedInput)>>();
            builder.RegisterType<ComputedInterceptor>();

            // Services
            builder.RegisterType<TimeProvider>()
                .As<ITimeProvider>()
                .SingleInstance();
            builder.RegisterType<TimeProvider>()
                .As<ITimeProviderEx>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(ComputedInterceptor))
                .SingleInstance();
        }
    }
}
