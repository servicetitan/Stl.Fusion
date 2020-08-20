using System;

namespace Stl.Fusion
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CacheAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public Type? CacheType { get; set; } = null;
        // In seconds, NaN means "use default"
        public double ExpirationTime { get; set; } = Double.NaN;
        public double OutputReleaseTime { get; set; } = Double.NaN;

        public CacheAttribute() { }
        public CacheAttribute(bool isEnabled)
            => IsEnabled = isEnabled;
        public CacheAttribute(double expirationTime, double outputReleaseTime = Double.NaN)
            : this(null!, expirationTime, outputReleaseTime) { }
        public CacheAttribute(Type cacheType, double expirationTime = Double.NaN, double outputReleaseTime = Double.NaN)
        {
            CacheType = cacheType;
            ExpirationTime = expirationTime;
            OutputReleaseTime = outputReleaseTime;
        }
    }
}
