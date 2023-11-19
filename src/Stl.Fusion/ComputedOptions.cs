using System.Diagnostics.CodeAnalysis;
using Stl.Interception.Internal;

namespace Stl.Fusion;

public record ComputedOptions
{
    public static ComputedOptions Default { get; set; } = new();
    public static ComputedOptions ClientDefault { get; set; } = new() {
        MinCacheDuration = TimeSpan.FromMinutes(1),
        ClientCacheMode = ClientCacheMode.Cache,
    };
    public static ComputedOptions MutableStateDefault { get; set; } = new() {
        TransientErrorInvalidationDelay = TimeSpan.MaxValue,
    };

    public TimeSpan MinCacheDuration { get; init; }
    public TimeSpan TransientErrorInvalidationDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan AutoInvalidationDelay { get; init; } = TimeSpan.MaxValue; // No auto invalidation
    public TimeSpan InvalidationDelay { get; init; }
    public ClientCacheMode ClientCacheMode { get; init; } = ClientCacheMode.NoCache;

    public static ComputedOptions? Get(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        MethodInfo method)
    {
        var isClientServiceMethod = type.IsInterface || typeof(InterfaceProxy).IsAssignableFrom(type);
#pragma warning disable IL2026
        var cma = method.GetAttribute<ComputeMethodAttribute>(true, true);
        var rma = isClientServiceMethod
            ? method.GetAttribute<ClientComputeMethodAttribute>(true, true)
            : null;
#pragma warning restore IL2026
        var a = rma ?? cma;
        if (a == null)
            return null;

        var defaultOptions = isClientServiceMethod ? ClientDefault : Default;
        // (Auto)InvalidationDelay for replicas should be taken from ReplicaMethodAttribute only
        var autoInvalidationDelay = isClientServiceMethod
            ? rma?.AutoInvalidationDelay ?? double.NaN
            : a.AutoInvalidationDelay;
        var invalidationDelay = isClientServiceMethod
            ? rma?.InvalidationDelay ?? double.NaN
            : a.InvalidationDelay;
        // Default cache behavior must be changed to null to let it "inherit" defaultOptions.ClientCacheMode
        var rmaCacheMode = rma?.ClientCacheMode;
        if (rmaCacheMode == ClientCacheMode.Default)
            rmaCacheMode = null;

        var options = new ComputedOptions() {
            MinCacheDuration = ToTimeSpan(a.MinCacheDuration) ?? defaultOptions.MinCacheDuration,
            TransientErrorInvalidationDelay = ToTimeSpan(a.TransientErrorInvalidationDelay) ?? defaultOptions.TransientErrorInvalidationDelay,
            AutoInvalidationDelay = ToTimeSpan(autoInvalidationDelay) ?? defaultOptions.AutoInvalidationDelay,
            InvalidationDelay = ToTimeSpan(invalidationDelay) ?? defaultOptions.InvalidationDelay,
            ClientCacheMode = rmaCacheMode ?? defaultOptions.ClientCacheMode,
        };
        return options == defaultOptions ? defaultOptions : options;
    }

    // Private methods

    private static TimeSpan? ToTimeSpan(double value)
    {
        if (double.IsNaN(value))
            return null;
        if (value >= TimeSpanExt.InfiniteInSeconds)
            return TimeSpan.MaxValue;
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        return TimeSpan.FromSeconds(value);
    }
}
