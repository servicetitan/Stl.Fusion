using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Stl.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(logging => {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddDebug();
            logging.Services.AddSingleton<ILoggerProvider>(c => {
                var outputAccessor = c.GetRequiredService<ITestOutputHelperAccessor>();
                return new XunitTestOutputLoggerProvider(
                    outputAccessor,
                    (category, level) => level >= LogLevel.Debug);
            });
        });
    }
}
