using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Stl.Fusion.Tests;

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
#pragma warning disable CS0618
                return new XunitTestOutputLoggerProvider(
                    outputAccessor,
                    (_, level) => level >= LogLevel.Debug);
#pragma warning restore CS0618
            });
        });
    }
}
