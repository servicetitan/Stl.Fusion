using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;
using Stl.IO;
using Stl.RegisterAttributes;
using Stl.Testing;
using Stl.Testing.Output;
using Stl.Tests.CommandR.Services;
using Xunit.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.CommandR
{
    public class CommandRTestBase : TestBase
    {
        protected bool UseDbContext { get; set; }
        protected Func<CommandHandler, Type, bool>? CommandHandlerFilter { get; set; }

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

            var commander = services.AddCommander();
            if (CommandHandlerFilter != null)
                commander.AddHandlerFilter(CommandHandlerFilter);

            var fusion = services.AddFusion();

            if (UseDbContext) {
                var testType = GetType();
                var appTempDir = FilePath.GetApplicationTempDirectory("", true);
                var dbPath = appTempDir & FilePath.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
                services.AddPooledDbContextFactory<TestDbContext>(builder => {
                    builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
                }, 256);
                services.AddDbContextServices<TestDbContext>(dbServices => {
                    dbServices.AddOperations();
                    dbServices.AddEntityResolver<string, User>();
                });
            }

            services.UseRegisterAttributeScanner()
                .WithTypeFilter(GetType().Namespace!)
                .RegisterFrom(GetType().Assembly);
        }
    }
}
