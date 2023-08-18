namespace Stl.Net;

public readonly record struct RetryDelayLogger(
    string Action,
    string? Tag,
    ILogger? Log,
    LogLevel LogLevel = LogLevel.Information)
{
    public RetryDelayLogger(string action, ILogger? log, LogLevel logLevel = LogLevel.Information)
        : this(action, null, log, logLevel)
    { }

    public void LogError(Exception error)
    {
        if (Tag == null)
            Log?.LogError(error, "Can't {Action}: {Error}", Action, error.Message);
        else
            Log?.LogError(error, "{Tag}: can't {Action}: {Error}", Tag, Action, error.Message);
    }

    public void LogDelay(int tryIndex, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        if (Tag == null)
            Log.IfEnabled(LogLevel)?.Log(LogLevel, "Will {Action} in {DelayDuration} (#{TryIndex})",
                Action, duration.ToShortString(), tryIndex);
        else
            Log.IfEnabled(LogLevel)?.Log(LogLevel, "{Tag}: will {Action} in {DelayDuration} (#{TryIndex})",
                Tag, Action, duration.ToShortString(), tryIndex);
    }

    public void LogLimitExceeded()
    {
        if (Tag == null)
            Log?.LogWarning("Can't {Action}: retry limit exceeded", Action);
        else
            Log?.LogWarning("{Tag}: can't {Action}: retry limit exceeded", Tag, Action);
    }
}
