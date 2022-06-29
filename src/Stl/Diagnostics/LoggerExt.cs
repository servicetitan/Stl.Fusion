namespace Stl.Diagnostics;

public static class LoggerExt
{
    public static bool IsLogging(this ILogger log, LogLevel logLevel) 
        => logLevel != LogLevel.None && log.IsEnabled(logLevel);
}
