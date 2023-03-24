using Stl.Fusion.Interception;

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
    public Type ComputeMethodDefType { get; init; } = typeof(ComputeMethodDef);

    public static ComputedOptions? FromAttribute(
        ComputedOptions defaultOptions,
        ComputeMethodAttribute? attribute)
    {
        if (attribute == null)
            return null;
        var options = new ComputedOptions() {
            MinCacheDuration = ToTimeSpan(attribute.MinCacheDuration) ?? defaultOptions.MinCacheDuration,
            TransientErrorInvalidationDelay = ToTimeSpan(attribute.TransientErrorInvalidationDelay) ?? defaultOptions.TransientErrorInvalidationDelay,
            AutoInvalidationDelay = ToTimeSpan(attribute.AutoInvalidationDelay) ?? defaultOptions.AutoInvalidationDelay,
            InvalidationDelay = ToTimeSpan(attribute.InvalidationDelay) ?? defaultOptions.InvalidationDelay,
            ComputeMethodDefType = attribute.ComputeMethodDefType ?? defaultOptions.ComputeMethodDefType,
        };
        return options == defaultOptions ? defaultOptions : options;
    }

    internal static TimeSpan? ToTimeSpan(double value)
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
