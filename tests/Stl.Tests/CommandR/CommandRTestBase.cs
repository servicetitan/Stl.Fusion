using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.IO;
using Stl.Tests.CommandR.Services;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.CommandR;

public class CommandRTestBase(ITestOutputHelper @out) : TestBase(@out)
{
    protected bool UseDbContext { get; set; }
    protected Func<CommandHandler, Type, bool>? CommandHandlerFilter { get; set; }

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

            bool LogFilter(string? category, LogLevel level)
            {
                category ??= "";
                return debugCategories.Any(category.StartsWith) && level >= LogLevel.Debug;
            }

            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddDebug();
            // XUnit logging requires weird setup b/c otherwise it filters out
            // everything below LogLevel.Information
            logging.AddProvider(
#pragma warning disable CS0618
                new XunitTestOutputLoggerProvider(
                    new TestOutputHelperAccessor() { Output = Out },
                    LogFilter));
#pragma warning restore CS0618
        });

        var commander = services.AddCommander();
        if (CommandHandlerFilter != null)
            commander.AddHandlerFilter(CommandHandlerFilter);

        var fusion = services.AddFusion();

        if (UseDbContext) {
            var testType = GetType();
            var appTempDir = FilePath.GetApplicationTempDirectory("", true);
            var dbPath = appTempDir & FilePath.GetHashedName($"{testType.Name}_{testType.Namespace}.db");
            services.AddTransientDbContextFactory<TestDbContext>(db => {
                db.UseSqlite($"Data Source={dbPath}", sqlite => { });
            });
            services.AddDbContextServices<TestDbContext>(db => {
                db.AddOperations();
                db.AddEntityResolver<string, User>();
            });
        }

        services.AddSingleton<LogCommandHandler>();
        commander.AddHandlers<LogCommandHandler>();

        services.AddSingleton<LogEnterExitService>();
        commander.AddHandlers<LogEnterExitService>();

        services.AddSingleton<UserService>();
        commander.AddHandlers<UserService>();

        commander.AddService<IMathService, MathService>();
    }
}
