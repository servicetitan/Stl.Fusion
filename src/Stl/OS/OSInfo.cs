namespace Stl.OS;

public enum OSKind
{
    OtherUnix = 0,
    Windows,
    MacOS,
    Android,
    IOS,
    WebAssembly,
}

public static class OSInfo
{
    public static readonly OSKind Kind;
    public static readonly string UserHomePath;

    public static bool IsWebAssembly => Kind == OSKind.WebAssembly;
    public static bool IsWindows => Kind == OSKind.Windows;
    public static bool IsAndroid => Kind == OSKind.Android;
    public static bool IsIOS => Kind == OSKind.IOS;
    public static bool IsMacOS => Kind == OSKind.MacOS;
    public static bool IsOtherUnix => Kind == OSKind.OtherUnix;
    public static bool IsAnyUnix => Kind == OSKind.OtherUnix || IsMacOS;
    public static bool IsAnyClient => IsWebAssembly || IsAndroid || IsIOS;

    static OSInfo()
    {
#if NET5_0_OR_GREATER
        // WebAssembly w/ .NET 5.0
        if (OperatingSystem.IsBrowser()) {
            Kind = OSKind.WebAssembly;
            UserHomePath = "";
            return;
        }

        // Windows
        if (OperatingSystem.IsWindows()) {
            Kind = OSKind.Windows;
            UserHomePath = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            return;
        }

        // MacOS or Unix
        if (OperatingSystem.IsAndroid())
            Kind = OSKind.Android;
        else if (OperatingSystem.IsIOS())
            Kind = OSKind.IOS;
        else if (OperatingSystem.IsMacOS())
            Kind = OSKind.MacOS;
        else
            Kind = OSKind.OtherUnix;
        UserHomePath = Environment.GetEnvironmentVariable("HOME") ?? "";
#else
        // WebAssembly w/ .NET 5.0+
        if (StringComparer.Ordinal.Equals("browser", RuntimeInformation.OSDescription.ToLowerInvariant())) {
            Kind = OSKind.WebAssembly;
            UserHomePath = "";
            return;
        }

        // WebAssembly w/ .NET Core 3.1
        if (StringComparer.Ordinal.Equals("web", RuntimeInformation.OSDescription)
            && RuntimeInformation.FrameworkDescription.StartsWith("Mono", StringComparison.Ordinal)) {
            Kind = OSKind.WebAssembly;
            UserHomePath = "";
            return;
        }

        // Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Kind = OSKind.Windows;
            UserHomePath = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            return;
        }

        // MacOS or Unix
        Kind = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? OSKind.MacOS
            : OSKind.OtherUnix;
        UserHomePath = Environment.GetEnvironmentVariable("HOME") ?? "";
#endif
    }
}
