using Stl.Fusion.Interception;
using Stl.Fusion.Swapping;

namespace Stl.Fusion;

public record ComputedOptions
{
    public static readonly ComputedOptions Default =
        new(
            keepAliveTime: TimeSpan.Zero,
            errorAutoInvalidateTime: TimeSpan.FromSeconds(1),
            autoInvalidateTime: TimeSpan.MaxValue,
            swappingOptions: SwappingOptions.NoSwapping);
    public static readonly ComputedOptions NoAutoInvalidateOnError =
        new(
            keepAliveTime: Default.KeepAliveTime,
            errorAutoInvalidateTime: TimeSpan.MaxValue,
            autoInvalidateTime: Default.AutoInvalidateTime,
            swappingOptions: Default.SwappingOptions);

    public TimeSpan KeepAliveTime { get; }
    public TimeSpan ErrorAutoInvalidateTime { get; }
    public TimeSpan AutoInvalidateTime { get; }
    public SwappingOptions SwappingOptions { get; }
    public Type ComputeMethodDefType { get; }
    public bool IsAsyncComputed { get; }

    public ComputedOptions(
        TimeSpan keepAliveTime,
        TimeSpan errorAutoInvalidateTime,
        TimeSpan autoInvalidateTime,
        SwappingOptions swappingOptions,
        Type? computeMethodDefType = null)
    {
        KeepAliveTime = keepAliveTime;
        ErrorAutoInvalidateTime = errorAutoInvalidateTime;
        AutoInvalidateTime = autoInvalidateTime;
        if (ErrorAutoInvalidateTime > autoInvalidateTime)
            // It just doesn't make sense to keep it higher
            ErrorAutoInvalidateTime = autoInvalidateTime;
        SwappingOptions = swappingOptions.IsEnabled ? swappingOptions : SwappingOptions.NoSwapping;
        ComputeMethodDefType = computeMethodDefType ?? typeof(ComputeMethodDef);
        IsAsyncComputed = swappingOptions.IsEnabled;
    }

    public static ComputedOptions? FromAttribute(ComputeMethodAttribute? attribute, SwapAttribute? swapAttribute)
    {
        if (attribute == null || !attribute.IsEnabled)
            return null;
        var swappingOptions = SwappingOptions.FromAttribute(swapAttribute);
        var options = new ComputedOptions(
            ToTimeSpan(attribute.KeepAliveTime) ?? Default.KeepAliveTime,
            ToTimeSpan(attribute.ErrorAutoInvalidateTime) ?? Default.ErrorAutoInvalidateTime,
            ToTimeSpan(attribute.AutoInvalidateTime) ?? Default.AutoInvalidateTime,
            swappingOptions,
            attribute.ComputeMethodDefType);
        return options.IsDefault() ? Default : options;
    }

    internal static TimeSpan? ToTimeSpan(double? value)
    {
        if (!value.HasValue)
            return null;
        var v = value.GetValueOrDefault();
        if (double.IsNaN(v))
            return null;
        if (double.IsPositiveInfinity(v))
            return TimeSpan.MaxValue;
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        return TimeSpan.FromSeconds(v);
    }

    private bool IsDefault()
        =>  KeepAliveTime == Default.KeepAliveTime
            && ErrorAutoInvalidateTime == Default.ErrorAutoInvalidateTime
            && AutoInvalidateTime == Default.AutoInvalidateTime
            && ComputeMethodDefType == Default.ComputeMethodDefType
            && SwappingOptions == Default.SwappingOptions
            && IsAsyncComputed == Default.IsAsyncComputed
            ;
}
