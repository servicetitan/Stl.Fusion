using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Stl.Plugins.Internal
{
    public sealed class LoggingBuilder : ILoggingBuilder
    {
        public IServiceCollection Services { get; }

        public LoggingBuilder(IServiceCollection services) => Services = services;
    }
}
