using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Caching
{
    [Serializable]
    public class CacheOptions
    {
        public static readonly CacheOptions NoCache = new CacheOptions(false);
        public static readonly CacheOptions Default = new CacheOptions(true);

        public bool IsCachingEnabled { get; }
        public Type CacheType { get; }
        public TimeSpan? ExpirationTime { get; }

        [JsonConstructor]
        public CacheOptions(
            bool isCachingEnabled,
            Type? cacheType = null,
            TimeSpan? expirationTime = null)
        {
            IsCachingEnabled = isCachingEnabled;
            CacheType = cacheType ?? typeof(ICache);
            ExpirationTime = expirationTime;
        }

        public static CacheOptions FromAttribute(CacheAttribute? attribute)
        {
            if (attribute == null || !attribute.IsEnabled)
                return NoCache;
            var expirationTime = ToTimeSpan(attribute.ExpirationTime);
            var cacheType = attribute.CacheType ?? typeof(ICache);
            if (!expirationTime.HasValue && cacheType == typeof(ICache))
                return Default;
            return new CacheOptions(true, cacheType, expirationTime);
        }

        private static TimeSpan? ToTimeSpan(double value)
            => ComputedOptions.ToTimeSpan(value);
    }
}
