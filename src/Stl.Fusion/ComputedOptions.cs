using Stl.Fusion.Interception;
using Stl.Fusion.Swapping;

namespace Stl.Fusion;

public record ComputedOptions
{
    public static ComputedOptions Default { get; set; } = new();
    public static ComputedOptions MutableStateDefault { get; set; } = new() {
        TransientErrorInvalidationDelay = TimeSpan.MaxValue,
    };
    public static ComputedOptions ReplicaDefault { get; set; } = new();

    public TimeSpan KeepAliveTime { get; init; }
    public TimeSpan TransientErrorInvalidationDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan AutoInvalidationDelay { get; init; } = TimeSpan.MaxValue; // No auto invalidation
    public SwappingOptions SwappingOptions { get; init; } = SwappingOptions.NoSwapping;
    public Type ComputeMethodDefType { get; init; } = typeof(ComputeMethodDef);
    public bool IsAsyncComputed => SwappingOptions.IsEnabled;

    public static ComputedOptions? FromAttribute(
        ComputedOptions defaultOptions,
        ComputeMethodAttribute? attribute,
        SwapAttribute? swapAttribute)
    {
        if (attribute is not { IsEnabled: true })
            return null;
        var options = new ComputedOptions() {
            KeepAliveTime = ToTimeSpan(attribute.MinCacheDuration) ?? defaultOptions.KeepAliveTime,
            TransientErrorInvalidationDelay = ToTimeSpan(attribute.TransientErrorInvalidationDelay) ?? defaultOptions.TransientErrorInvalidationDelay,
            AutoInvalidationDelay = ToTimeSpan(attribute.AutoInvalidationDelay) ?? defaultOptions.AutoInvalidationDelay,
            SwappingOptions = SwappingOptions.FromAttribute(defaultOptions.SwappingOptions, swapAttribute),
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
