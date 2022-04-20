using System.Diagnostics.Metrics;

namespace Stl.Diagnostics;

public static class MeterExt
{
    public static Meter Unknown { get; } = new("<Unknown>");
}
