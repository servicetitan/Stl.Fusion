using System;
using Newtonsoft.Json;
using Stl.Fusion.Caching;

namespace Stl.Fusion
{
    [Serializable]
    public class ComputedOptions
    {
        public static readonly TimeSpan DefaultCacheKeepAliveTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultKeepAliveTime = TimeSpan.Zero;
        public static readonly TimeSpan DefaultErrorAutoInvalidateTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan DefaultAutoInvalidateTime = TimeSpan.MaxValue; // No auto invalidation

        public static readonly ComputedOptions Default = new ComputedOptions();
        public static readonly ComputedOptions NoAutoInvalidateOnError =
            new ComputedOptions(errorAutoInvalidateTime: TimeSpan.MaxValue);

        public TimeSpan KeepAliveTime { get; }
        public TimeSpan ErrorAutoInvalidateTime { get; }
        public TimeSpan AutoInvalidateTime { get; }
        public CacheOptions CacheOptions { get; }
        [JsonIgnore]
        public bool IsCachingEnabled { get; }

        public ComputedOptions(
            TimeSpan? keepAliveTime = null,
            TimeSpan? errorAutoInvalidateTime = null,
            TimeSpan? autoInvalidateTime = null,
            CacheOptions? cacheOptions = null)
            : this(
                keepAliveTime ?? ((cacheOptions ?? CacheOptions.NoCache).IsCachingEnabled
                    ? DefaultCacheKeepAliveTime
                    : DefaultKeepAliveTime),
                errorAutoInvalidateTime ?? DefaultErrorAutoInvalidateTime,
                autoInvalidateTime ?? DefaultAutoInvalidateTime,
                cacheOptions ?? CacheOptions.NoCache)
        { }

        [JsonConstructor]
        public ComputedOptions(
            TimeSpan keepAliveTime,
            TimeSpan errorAutoInvalidateTime,
            TimeSpan autoInvalidateTime,
            CacheOptions cacheOptions)
        {
            KeepAliveTime = keepAliveTime;
            ErrorAutoInvalidateTime = errorAutoInvalidateTime;
            AutoInvalidateTime = autoInvalidateTime;
            if (ErrorAutoInvalidateTime > autoInvalidateTime)
                // It just doesn't make sense to keep it higher
                ErrorAutoInvalidateTime = autoInvalidateTime;
            CacheOptions = cacheOptions.IsCachingEnabled ? cacheOptions : CacheOptions.NoCache;
            IsCachingEnabled = CacheOptions.IsCachingEnabled;
        }

        public static ComputedOptions FromAttribute(InterceptedMethodAttribute? attribute, CacheAttribute? cacheAttribute)
        {
            var cacheOptions = CacheOptions.FromAttribute(cacheAttribute);
            var computeMethodAttribute = attribute as ComputeMethodAttribute;
            if (computeMethodAttribute == null && !cacheOptions.IsCachingEnabled)
                return Default;
            return new ComputedOptions(
                ToTimeSpan(computeMethodAttribute?.KeepAliveTime),
                ToTimeSpan(computeMethodAttribute?.ErrorAutoInvalidateTime),
                ToTimeSpan(computeMethodAttribute?.AutoInvalidateTime),
                cacheOptions);
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
    }
}
