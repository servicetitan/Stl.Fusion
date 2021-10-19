using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping;

public class LoggingSwapServiceWrapper<TSwapService> : ISwapService
    where TSwapService : ISwapService
{
    public class Options
    {
        public bool IsLoggingEnabled { get; set; } = true;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }

    protected readonly TSwapService SwapService;
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; }

    public LoggingSwapServiceWrapper(
        TSwapService swapService,
        ILoggerFactory? loggerFactory = null)
        : this(null, swapService, loggerFactory)
    { }

    public LoggingSwapServiceWrapper(
        Options? options,
        TSwapService swapService,
        ILoggerFactory? loggerFactory = null)
    {
        options ??= new();
        loggerFactory ??= NullLoggerFactory.Instance;
        Log = loggerFactory.CreateLogger(swapService.GetType());
        LogLevel = options.LogLevel;
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);

        SwapService = swapService;
    }

    public async ValueTask<IResult?> Load((ComputeMethodInput Input, LTag Version) key, CancellationToken cancellationToken = default)
    {
        var value = await SwapService.Load(key, cancellationToken).ConfigureAwait(false);
        if (IsLoggingEnabled)
            Log.Log(LogLevel, "[?] {Key} -> {Value}", key, value);
        return value;
    }

    public ValueTask Store((ComputeMethodInput Input, LTag Version) key, IResult value,
        CancellationToken cancellationToken = default)
    {
        if (IsLoggingEnabled)
            Log.Log(LogLevel, "[=] {Key} <- {Value}", key, value);
        return SwapService.Store(key, value, cancellationToken);
    }
}
