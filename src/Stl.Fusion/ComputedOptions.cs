using System;
using Newtonsoft.Json;
using Stl.Fusion.Swapping;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly ComputedOptions Default =
            new ComputedOptions(
                keepAliveTime: TimeSpan.Zero,
                errorAutoInvalidateTime: TimeSpan.FromSeconds(1),
                autoInvalidateTime: TimeSpan.MaxValue,
                swappingOptions: SwappingOptions.NoSwapping);
        public static readonly ComputedOptions NoAutoInvalidateOnError =
            new ComputedOptions(
                keepAliveTime: Default.KeepAliveTime,
                errorAutoInvalidateTime: TimeSpan.MaxValue,
                autoInvalidateTime: Default.AutoInvalidateTime,
                swappingOptions: Default.SwappingOptions);

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTime { get; }
        public TimeSpan AutoInvalidateTime { get; }
        public SwappingOptions SwappingOptions { get; }
        [JsonIgnore]
        public bool IsAsyncComputed { get; }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime,
            TimeSpan errorAutoInvalidateTime,
            TimeSpan autoInvalidateTime,
            SwappingOptions swappingOptions,
            Type? argumentDefaultProvider = null)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTime = errorAutoInvalidateTime;
            AutoInvalidateTime = autoInvalidateTime;
            if (ErrorAutoInvalidateTime > autoInvalidateTime)
                // It just doesn't make sense to keep it higher
                ErrorAutoInvalidateTime = autoInvalidateTime;
            SwappingOptions = swappingOptions.IsEnabled ? swappingOptions : SwappingOptions.NoSwapping;
            IsAsyncComputed = swappingOptions.IsEnabled;
        }

        public static ComputedOptions FromAttribute(InterceptedMethodAttribute? attribute, SwapAttribute? swapAttribute)
        {
            var swappingOptions = SwappingOptions.FromAttribute(swapAttribute);
            var cma = attribute as ComputeMethodAttribute;
            if (cma == null && !swappingOptions.IsEnabled)
                return Default;
            var options = new ComputedOptions(
                ToTimeSpan(cma?.KeepAliveTime) ?? Default.KeepAliveTime,
                ToTimeSpan(cma?.ErrorAutoInvalidateTime) ?? Default.ErrorAutoInvalidateTime,
                ToTimeSpan(cma?.AutoInvalidateTime) ?? Default.AutoInvalidateTime,
                swappingOptions);
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
                && SwappingOptions == Default.SwappingOptions
                && IsAsyncComputed == Default.IsAsyncComputed;
    }
}
