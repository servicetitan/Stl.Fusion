using System;

namespace Stl.Fusion.Caching
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CachingAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public Type? CacheType { get; set; } = null;
        // In seconds, NaN means "use default"
        public double ExpirationTime { get; set; } = Double.NaN;
        public double OutputReleaseTime { get; set; } = Double.NaN;

        public CachingAttribute() { }
        public CachingAttribute(bool isEnabled)
            => IsEnabled = isEnabled;
        public CachingAttribute(double expirationTime, double outputReleaseTime = Double.NaN)
            : this(null!, expirationTime, outputReleaseTime) { }
        public CachingAttribute(Type cacheType, double expirationTime = Double.NaN, double outputReleaseTime = Double.NaN)
        {
            CacheType = cacheType;
            ExpirationTime = expirationTime;
            OutputReleaseTime = outputReleaseTime;
        }
    }
}
