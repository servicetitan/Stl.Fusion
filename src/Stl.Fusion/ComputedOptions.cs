using Stl.Interception.Internal;

namespace Stl.Fusion;

public record ComputedOptions
{
    public static ComputedOptions Default { get; set; } = new();
    public static ComputedOptions ReplicaDefault { get; set; } = new() {
        MinCacheDuration = TimeSpan.FromMinutes(1),
    };
    public static ComputedOptions MutableStateDefault { get; set; } = new() {
        TransientErrorInvalidationDelay = TimeSpan.MaxValue,
    };

    public TimeSpan MinCacheDuration { get; init; }
    public TimeSpan TransientErrorInvalidationDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan AutoInvalidationDelay { get; init; } = TimeSpan.MaxValue; // No auto invalidation
    public TimeSpan InvalidationDelay { get; init; }
    public ReplicaCacheBehavior ReplicaCacheBehavior { get; init; }

    public static ComputedOptions? Get(Type type, MethodInfo method)
    {
        var isReplicaServiceMethod = type.IsInterface || typeof(InterfaceProxy).IsAssignableFrom(type);
        var cma = method.GetAttribute<ComputeMethodAttribute>(true, true);
        var rma = isReplicaServiceMethod ? method.GetAttribute<ReplicaMethodAttribute>(true, true) : null;
        var attr = rma ?? cma;
        if (attr == null)
            return null;

        var defaultOptions = isReplicaServiceMethod ? ReplicaDefault : Default;
        var autoInvalidationDelay = isReplicaServiceMethod
            ? rma?.AutoInvalidationDelay ?? double.NaN
            : attr.AutoInvalidationDelay;
        var invalidationDelay = isReplicaServiceMethod
            ? rma?.InvalidationDelay ?? double.NaN
            : attr.InvalidationDelay;
        var options = new ComputedOptions() {
            MinCacheDuration = ToTimeSpan(attr.MinCacheDuration) ?? defaultOptions.MinCacheDuration,
            TransientErrorInvalidationDelay = ToTimeSpan(attr.TransientErrorInvalidationDelay) ?? defaultOptions.TransientErrorInvalidationDelay,
            AutoInvalidationDelay = ToTimeSpan(autoInvalidationDelay) ?? defaultOptions.AutoInvalidationDelay,
            InvalidationDelay = ToTimeSpan(invalidationDelay) ?? defaultOptions.InvalidationDelay,
            ReplicaCacheBehavior = rma?.CacheBehavior ?? defaultOptions.ReplicaCacheBehavior,
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
