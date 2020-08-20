using System;
using Newtonsoft.Json;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Caching
{
    [Serializable]
    public class CachingOptions
    {
        public static readonly CachingOptions Default =
            new CachingOptions(
                isCachingEnabled: true,
                cacheType: typeof(ICache<InterceptedInput>),
                expirationTime: TimeSpan.Zero,
                outputReleaseTime: TimeSpan.FromSeconds(10));
        public static readonly CachingOptions NoCaching =
            new CachingOptions(false, Default.CacheType, Default.ExpirationTime, Default.OutputReleaseTime);

        public bool IsCachingEnabled { get; }
        public Type CacheType { get; }
        public TimeSpan ExpirationTime { get; }
        public TimeSpan OutputReleaseTime { get; }

        [JsonConstructor]
        public CachingOptions(
            bool isCachingEnabled,
            Type cacheType,
            TimeSpan expirationTime,
            TimeSpan outputReleaseTime)
        {
            IsCachingEnabled = isCachingEnabled;
            CacheType = cacheType;
            ExpirationTime = expirationTime;
            OutputReleaseTime = outputReleaseTime;
        }

        public static CachingOptions FromAttribute(CacheAttribute? attribute)
        {
            if (attribute == null || !attribute.IsEnabled)
                return NoCaching;
            var cacheType = attribute.CacheType ?? Default.CacheType;
            var expirationTime = ToTimeSpan(attribute.ExpirationTime) ?? Default.ExpirationTime;
            var outputReleaseTime = ToTimeSpan(attribute.OutputReleaseTime) ?? Default.OutputReleaseTime;
            var options = new CachingOptions(true, cacheType, expirationTime, outputReleaseTime);
            return options.IsDefault() ? Default : options;
        }

        private static TimeSpan? ToTimeSpan(double value)
            => ComputedOptions.ToTimeSpan(value);

        private bool IsDefault()
            =>  Default.IsCachingEnabled
                && ExpirationTime == Default.ExpirationTime
                && OutputReleaseTime == Default.OutputReleaseTime
                && CacheType == Default.CacheType;
    }
}
