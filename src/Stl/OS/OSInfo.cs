namespace Stl.OS;

public enum OSKind
{
    OtherUnix = 0,
    Windows = 1,
    MacOS = 2,
    WebAssembly = 4,
}

public static class OSInfo
{
    public static readonly OSKind Kind;
    public static readonly string UserHomePath;

    public static bool IsWebAssembly => Kind == OSKind.WebAssembly;
    public static bool IsWindows => Kind == OSKind.Windows;
    public static bool IsMacOS => Kind == OSKind.MacOS;
    public static bool IsOtherUnix => Kind == OSKind.OtherUnix;
    public static bool IsAnyUnix => Kind == OSKind.OtherUnix || IsMacOS;

    static OSInfo()
    {
        // WebAssembly w/ .NET 5.0
        if (RuntimeInformation.OSDescription.ToLowerInvariant() == "browser") {
            Kind = OSKind.WebAssembly;
            UserHomePath = "";
            return;
        }

        // WebAssembly w/ .NET Core 3.1
        if (RuntimeInformation.OSDescription == "web" && RuntimeInformation.FrameworkDescription.StartsWith("Mono")) {
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

        // Unix
        Kind = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? OSKind.MacOS
            : OSKind.OtherUnix;
        UserHomePath = Environment.GetEnvironmentVariable("HOME") ?? "";
    }
}
