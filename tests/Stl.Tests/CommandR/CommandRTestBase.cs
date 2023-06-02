using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.IO;
using Stl.RegisterAttributes;
using Stl.Testing.Output;
using Stl.Tests.CommandR.Services;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests.CommandR;

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
#pragma warning disable CS0618
            logging.AddProvider(new XunitTestOutputLoggerProvider(
                new TestOutputHelperAccessor(Out),
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
            services.AddPooledDbContextFactory<TestDbContext>(db => {
                db.UseSqlite($"Data Source={dbPath}", sqlite => { });
            }, 256);
            services.AddDbContextServices<TestDbContext>(db => {
                db.AddOperations();
                db.AddEntityResolver<string, User>();
            });
        }

        services.UseRegisterAttributeScanner()
            .WithTypeFilter(GetType().Namespace!)
            .RegisterFrom(GetType().Assembly);
    }
}
