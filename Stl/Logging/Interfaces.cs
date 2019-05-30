using Microsoft.Extensions.Logging;

namespace Stl
{
    public interface IHasLogger
    {
        ILogger Logger { get; }
    }

    public interface IHasLoggerFactory
    {
        ILoggerFactory LoggerFactory { get; }
    }
}
