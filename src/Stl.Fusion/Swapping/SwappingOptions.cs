namespace Stl.Fusion.Swapping;

public record SwappingOptions
{
    public static SwappingOptions NoSwapping { get; } = new() { IsEnabled = false };

    public bool IsEnabled { get; init; } = true;
    public Type SwapServiceType { get; init; } = typeof(ISwapService);
    public TimeSpan SwapDelay { get; init; } = TimeSpan.FromSeconds(10);

    public static SwappingOptions FromAttribute(SwappingOptions defaultOptions, SwapAttribute? attribute)
    {
        if (attribute is not { IsEnabled: true })
            return NoSwapping;
        var options = new SwappingOptions() {
            IsEnabled = true,
            SwapServiceType = attribute.SwapServiceType ?? defaultOptions.SwapServiceType,
            SwapDelay = ToTimeSpan(attribute.SwapDelay) ?? defaultOptions.SwapDelay,
        };
        return options == defaultOptions ? defaultOptions : options;
    }

    private static TimeSpan? ToTimeSpan(double value)
        => ComputedOptions.ToTimeSpan(value);
}
