using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stl.CommandR.Diagnostics;

public static class CommanderDiagnostics
{
    public static ActivitySource CommanderTrace { get; } =
        new("Stl.CommandR", ThisAssembly.AssemblyInformationalVersion);
    public static Meter CommanderMeter { get; } =
        new("Stl.CommandR", ThisAssembly.AssemblyInformationalVersion);
}
