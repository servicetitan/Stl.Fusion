namespace Stl.Fusion.Swapping;

public class SwappingOptions
{
    public static readonly SwappingOptions Default =
        new(
            isEnabled: true,
            swapServiceType: typeof(ISwapService),
            swapTime: TimeSpan.FromSeconds(10));
    public static readonly SwappingOptions NoSwapping =
        new(
            isEnabled: false,
            swapServiceType: Default.SwapServiceType,
            swapTime: Default.SwapTime);

    public bool IsEnabled { get; }
    public Type SwapServiceType { get; }
    public TimeSpan SwapTime { get; }

    public SwappingOptions(
        bool isEnabled,
        Type swapServiceType,
        TimeSpan swapTime)
    {
        IsEnabled = isEnabled;
        SwapServiceType = swapServiceType;
        SwapTime = swapTime;
    }

    public static SwappingOptions FromAttribute(SwapAttribute? attribute)
    {
        if (attribute == null || !attribute.IsEnabled)
            return NoSwapping;
        var cacheType = attribute.SwapServiceType ?? Default.SwapServiceType;
        var swapTime = ToTimeSpan(attribute.SwapTime) ?? Default.SwapTime;
        var options = new SwappingOptions(true, cacheType, swapTime);
        return options.IsDefault() ? Default : options;
    }

    private static TimeSpan? ToTimeSpan(double value)
        => ComputedOptions.ToTimeSpan(value);

    private bool IsDefault()
        =>  Default.IsEnabled
            && SwapTime == Default.SwapTime
            && SwapServiceType == Default.SwapServiceType;
}
