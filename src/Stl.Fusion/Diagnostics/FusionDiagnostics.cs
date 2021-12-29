using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stl.Fusion.Diagnostics;

public static class FusionDiagnostics
{
    public static ActivitySource FusionTrace { get; } =
        new("Stl.Fusion", ThisAssembly.AssemblyInformationalVersion);
    public static Meter FusionMeter { get; } =
        new("Stl.Fusion", ThisAssembly.AssemblyInformationalVersion);
}
