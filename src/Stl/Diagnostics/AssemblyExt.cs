using System.Diagnostics;
using System.Diagnostics.Metrics;
using Stl.OS;

namespace Stl.Diagnostics;

public static class AssemblyExt
{
    private static readonly ConcurrentDictionary<Assembly, string?> InformationalVersions = new();
    private static readonly ConcurrentDictionary<Assembly, ActivitySource> ActivitySources = new();
    private static readonly ConcurrentDictionary<Assembly, Meter> Meters = new();
    private static readonly string UnknownName = "<Unknown>";
    private static readonly string UnknownVersion = "<Unknown Version>";

    public static string? GetInformationalVersion(this Assembly assembly)
        => InformationalVersionResolver.Invoke(assembly);

    public static ActivitySource GetActivitySource(this Assembly assembly)
        => ActivitySourceResolver.Invoke(assembly);
    public static Meter GetMeter(this Assembly assembly)
        => MeterResolver.Invoke(assembly);

    // Overridable part

    public static Func<Assembly, string?> InformationalVersionResolver { get; set; } =
        assembly => InformationalVersions.GetOrAdd(assembly,
            static a => {
                var attrs = (AssemblyInformationalVersionAttribute[])a
                    .GetCustomAttributes(
                        typeof(AssemblyInformationalVersionAttribute),
                        inherit: false);
                var version = attrs.FirstOrDefault()?.InformationalVersion;
                if (!version.IsNullOrEmpty())
                    return version;

                if (OSInfo.IsWebAssembly)
                    return null;

                try {
                    version = FileVersionInfo.GetVersionInfo(a.Location).ProductVersion;
                    return version.NullIfEmpty();
                }
                catch {
                    return null;
                }
            });

    public static Func<Assembly, ActivitySource> ActivitySourceResolver { get; set; } =
        assembly => ActivitySources.GetOrAdd(assembly,
            static a => new ActivitySource(
                a.GetName().Name ?? UnknownName,
                a.GetInformationalVersion() ?? UnknownVersion));

    public static Func<Assembly, Meter> MeterResolver { get; set; } =
        assembly => Meters.GetOrAdd(assembly,
            static a => new Meter(
                a.GetName().Name ?? UnknownName,
                a.GetInformationalVersion() ?? UnknownVersion));
}
