using System;

namespace Stl.Fusion
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class SwapAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public Type? SwapServiceType { get; set; }
        // In seconds, NaN means "use default"
        public double SwapTime { get; set; } = Double.NaN;

        public SwapAttribute() { }
        public SwapAttribute(bool isEnabled)
            => IsEnabled = isEnabled;
        public SwapAttribute(double swapTime)
            : this(null!, swapTime) { }
        public SwapAttribute(Type swapServiceType, double swapTime = Double.NaN)
        {
            SwapServiceType = swapServiceType;
            SwapTime = swapTime;
        }
    }
}
