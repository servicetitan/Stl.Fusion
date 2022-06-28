using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping;

public class LoggingSwapServiceWrapper<TSwapService> : ISwapService
    where TSwapService : ISwapService
{
    public record Options
    {
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    protected Options Settings { get; }
    protected TSwapService SwapService { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; }

    public LoggingSwapServiceWrapper(
        Options settings,
        TSwapService swapService,
        ILoggerFactory? loggerFactory = null)
    {
        Settings = settings;
        loggerFactory ??= NullLoggerFactory.Instance;
        Log = loggerFactory.CreateLogger(swapService.GetType());
        IsLoggingEnabled = Log.IsLogging(settings.LogLevel);

        SwapService = swapService;
    }

    public async ValueTask<IResult?> Load((ComputeMethodInput Input, LTag Version) key, CancellationToken cancellationToken = default)
    {
        var value = await SwapService.Load(key, cancellationToken).ConfigureAwait(false);
        if (IsLoggingEnabled)
            Log.Log(Settings.LogLevel, "[?] {Key} -> {Value}", key, value);
        return value;
    }

    public ValueTask Store((ComputeMethodInput Input, LTag Version) key, IResult value,
        CancellationToken cancellationToken = default)
    {
        if (IsLoggingEnabled)
            Log.Log(Settings.LogLevel, "[=] {Key} <- {Value}", key, value);
        return SwapService.Store(key, value, cancellationToken);
    }
}
