using System.Diagnostics.CodeAnalysis;

namespace Stl.Diagnostics;

public static class LoggerExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETSTANDARD2_0
    public static bool IsLogging(this ILogger? log, LogLevel logLevel)
#else
    public static bool IsLogging([NotNullWhen(true)] this ILogger? log, LogLevel logLevel)
#endif
        => logLevel != LogLevel.None && log?.IsEnabled(logLevel) == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILogger? IfEnabled(this ILogger? log, LogLevel logLevel)
        => IsLogging(log, logLevel) ? log : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILogger? IfEnabled(this ILogger? log, LogLevel logLevel, bool isEnabled)
        => isEnabled ? log?.IfEnabled(logLevel) : null;
}
