using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Extensibility;
using Stl.Fusion.EntityFramework;
using Stl.IO;
using Stl.Testing;
using Stl.Testing.Internal;
using Stl.Tests.CommandR.Services;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.CommandR
{
    public class CommandRTestBase : TestBase
    {
        protected bool UseDbContext { get; set; }

        public CommandRTestBase(ITestOutputHelper @out) : base(@out) { }

        protected virtual IServiceProvider CreateServices()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var services = serviceCollection.BuildServiceProvider();

            if (UseDbContext) {
                var dbContextFactory = services.GetRequiredService<IDbContextFactory<TestDbContext>>();
                using var dbContext = dbContextFactory.CreateDbContext();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }
            return services;
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(logging => {
                var debugCategories = new List<string> {
                    "Stl.CommandR",
                    "Stl.Tests.CommandR",
                };

                bool LogFilter(string category, LogLevel level)
                    => debugCategories.Any(category.StartsWith) && level >= LogLevel.Debug;

                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
                // XUnit logging requires weird setup b/c otherwise it filters out
                // everything below LogLevel.Information
                logging.AddProvider(new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor(Out),
                    LogFilter));
            });

            var commandR = services.AddCommander();

            if (UseDbContext) {
                var testType = GetType();
                var appTempDir = PathEx.GetApplicationTempDirectory("", true);
                var dbPath = appTempDir & PathEx.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
                services.AddPooledDbContextFactory<TestDbContext>(builder => {
                    builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
                }, 256);
                services.AddDbContextServices<TestDbContext>(dbServices => {
                    dbServices.AddDbOperations();
                    dbServices.AddDbEntityResolver<string, User>();
                });
            }

            // [Module] attribute test
            services.UseModules(); // Just to check you can call it twice
            services.UseModules(b => b.ConfigureModuleServices(s => {
                s.UseAttributeScanner(ModuleAttribute.DefaultScope)
                    .WithTypeFilter(GetType().Namespace!)
                    .AddServicesFrom(Assembly.GetExecutingAssembly());
            }));
        }
    }
}
