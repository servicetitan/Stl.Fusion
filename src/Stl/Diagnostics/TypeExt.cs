using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stl.Diagnostics;

public static class TypeExt
{
    public static string GetOperationName(this Type type, string operation)
        => $"{operation}@{type.NonProxyType().GetName()}";

    public static ActivitySource GetActivitySource(this Type type)
        => ActivitySourceResolver.Invoke(type);
    public static Meter GetMeter(this Type type)
        => MeterResolver.Invoke(type);

    // Overridable part

    public static Func<Type, ActivitySource> ActivitySourceResolver { get; set; } =
        type => type.NonProxyType().Assembly.GetActivitySource();
    public static Func<Type, Meter> MeterResolver { get; set; } =
        type => type.NonProxyType().Assembly.GetMeter();
}
