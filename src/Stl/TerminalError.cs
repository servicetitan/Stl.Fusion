namespace Stl;

public delegate bool TerminalErrorDetector(Exception exception, CancellationToken cancellationToken);

public static class TerminalError
{
    public static TerminalErrorDetector Detector { get; set; }
        = static (e, ct) => e.IsCancellationOf(ct) || e.Any(IsServiceProviderDisposedException);

    public static bool IsServiceProviderDisposedException(Exception error)
    {
        if (error is not ObjectDisposedException ode)
            return false;
        if (Equals(error.GetType().Name, "JSDisconnectedException"))
            return true; // This is specific to Blazor Server, it also indicates the scope is going to die soon

#if NETSTANDARD2_0
        return ode.ObjectName.Contains("IServiceProvider")
            || ode.Message.Contains("'IServiceProvider'");
#else
        return ode.ObjectName.Contains("IServiceProvider", StringComparison.Ordinal)
            || ode.Message.Contains("'IServiceProvider'", StringComparison.Ordinal);
#endif
    }
}
