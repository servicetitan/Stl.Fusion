using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stl.Diagnostics;

public static class StlDiagnostics
{
    public static ActivitySource StlTrace { get; } =
        new("Stl", ThisAssembly.AssemblyInformationalVersion);
    public static Meter StlMeter { get; } =
        new("Stl", ThisAssembly.AssemblyInformationalVersion);

    public static ActivitySource UnspecifiedTrace { get; } =
        new("Unspecified", ThisAssembly.AssemblyInformationalVersion);
    public static Meter UnspecifiedMeter { get; } =
        new("Unspecified", ThisAssembly.AssemblyInformationalVersion);
}
