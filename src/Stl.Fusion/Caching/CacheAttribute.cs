using System;

namespace Stl.Fusion.Caching
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CacheAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public Type? CacheType { get; set; } = null;
        // In seconds, NaN means "use default"
        public double ExpirationTime { get; set; } = Double.NaN;

        public CacheAttribute() { }
        public CacheAttribute(bool isEnabled)
            => IsEnabled = isEnabled;
        public CacheAttribute(double expirationTime)
            : this(null!, expirationTime) { }
        public CacheAttribute(Type cacheType, double expirationTime = Double.NaN)
        {
            CacheType = cacheType;
            ExpirationTime = expirationTime;
        }
    }
}
