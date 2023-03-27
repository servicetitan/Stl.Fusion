using Stl.Interception.Internal;

namespace Stl.Fusion;

public record ComputedOptions
{
    public static ComputedOptions Default { get; set; } = new();
    public static ComputedOptions ReplicaDefault { get; set; } = new() {
        MinCacheDuration = TimeSpan.FromMinutes(1),
        ReplicaCacheBehavior = ReplicaCacheBehavior.Standard,
    };
    public static ComputedOptions MutableStateDefault { get; set; } = new() {
        TransientErrorInvalidationDelay = TimeSpan.MaxValue,
    };

    public TimeSpan MinCacheDuration { get; init; }
    public TimeSpan TransientErrorInvalidationDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan AutoInvalidationDelay { get; init; } = TimeSpan.MaxValue; // No auto invalidation
    public TimeSpan InvalidationDelay { get; init; }
    public ReplicaCacheBehavior ReplicaCacheBehavior { get; init; } = ReplicaCacheBehavior.None;

    public static ComputedOptions? Get(Type type, MethodInfo method)
    {
        var isReplicaServiceMethod = type.IsInterface || typeof(InterfaceProxy).IsAssignableFrom(type);
        var cma = method.GetAttribute<ComputeMethodAttribute>(true, true);
        var rma = isReplicaServiceMethod ? method.GetAttribute<ReplicaMethodAttribute>(true, true) : null;
        var a = rma ?? cma;
        if (a == null)
            return null;

        var defaultOptions = isReplicaServiceMethod ? ReplicaDefault : Default;
        // (Auto)InvalidationDelay for replicas should be taken from ReplicaMethodAttribute only 
        var autoInvalidationDelay = isReplicaServiceMethod
            ? rma?.AutoInvalidationDelay ?? double.NaN
            : a.AutoInvalidationDelay;
        var invalidationDelay = isReplicaServiceMethod
            ? rma?.InvalidationDelay ?? double.NaN
            : a.InvalidationDelay;
        // Default cache behavior must be changed to null to let it "inherit" defaultOptions.ReplicaCacheBehavior  
        var rmaCacheBehavior = rma?.CacheBehavior;
        if (rmaCacheBehavior == ReplicaCacheBehavior.Default)
            rmaCacheBehavior = null;

        var options = new ComputedOptions() {
            MinCacheDuration = ToTimeSpan(a.MinCacheDuration) ?? defaultOptions.MinCacheDuration,
            TransientErrorInvalidationDelay = ToTimeSpan(a.TransientErrorInvalidationDelay) ?? defaultOptions.TransientErrorInvalidationDelay,
            AutoInvalidationDelay = ToTimeSpan(autoInvalidationDelay) ?? defaultOptions.AutoInvalidationDelay,
            InvalidationDelay = ToTimeSpan(invalidationDelay) ?? defaultOptions.InvalidationDelay,
            ReplicaCacheBehavior = rmaCacheBehavior ?? defaultOptions.ReplicaCacheBehavior,
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
