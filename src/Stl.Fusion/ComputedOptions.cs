using System;
using Newtonsoft.Json;
using Stl.Fusion.Caching;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly ComputedOptions Default =
            new ComputedOptions(
                keepAliveTime: TimeSpan.FromSeconds(1),
                errorAutoInvalidateTime: TimeSpan.FromSeconds(1),
                autoInvalidateTime: TimeSpan.MaxValue,
                cachingOptions: CachingOptions.NoCaching);
        public static readonly ComputedOptions NoAutoInvalidateOnError =
            new ComputedOptions(
                keepAliveTime: Default.KeepAliveTime,
                errorAutoInvalidateTime: TimeSpan.MaxValue,
                autoInvalidateTime: Default.AutoInvalidateTime,
                cachingOptions: Default.CachingOptions);

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTime { get; }
        public TimeSpan AutoInvalidateTime { get; }
        public CachingOptions CachingOptions { get; }
        [JsonIgnore]
        public bool IsCachingEnabled { get; }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime,
            TimeSpan errorAutoInvalidateTime,
            TimeSpan autoInvalidateTime,
            CachingOptions cachingOptions)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTime = errorAutoInvalidateTime;
            AutoInvalidateTime = autoInvalidateTime;
            if (ErrorAutoInvalidateTime > autoInvalidateTime)
                // It just doesn't make sense to keep it higher
                ErrorAutoInvalidateTime = autoInvalidateTime;
            CachingOptions = cachingOptions.IsCachingEnabled ? cachingOptions : CachingOptions.NoCaching;
            IsCachingEnabled = CachingOptions.IsCachingEnabled;
        }

        public static ComputedOptions FromAttribute(InterceptedMethodAttribute? attribute, CachingAttribute? cacheAttribute)
        {
            var cacheOptions = CachingOptions.FromAttribute(cacheAttribute);
            var cma = attribute as ComputeMethodAttribute;
            if (cma == null && !cacheOptions.IsCachingEnabled)
                return Default;
            var options = new ComputedOptions(
                ToTimeSpan(cma?.KeepAliveTime) ?? Default.KeepAliveTime,
                ToTimeSpan(cma?.ErrorAutoInvalidateTime) ?? Default.ErrorAutoInvalidateTime,
                ToTimeSpan(cma?.AutoInvalidateTime) ?? Default.AutoInvalidateTime,
                cacheOptions);
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
            =>  Default.IsCachingEnabled
                && KeepAliveTime == Default.KeepAliveTime
                && ErrorAutoInvalidateTime == Default.ErrorAutoInvalidateTime
                && AutoInvalidateTime == Default.AutoInvalidateTime
                && CachingOptions == Default.CachingOptions;
    }
}
