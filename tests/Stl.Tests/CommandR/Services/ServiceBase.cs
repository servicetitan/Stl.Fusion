using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Tests.CommandR.Services;

public abstract class ServiceBase
{
    protected IServiceProvider Services { get; }
    protected ILogger Log { get; }

    protected ServiceBase(IServiceProvider services)
    {
        Services = services;
        Log = services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
    }
}
