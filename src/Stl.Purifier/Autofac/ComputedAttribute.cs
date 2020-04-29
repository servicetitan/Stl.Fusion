using System;

namespace Stl.Purifier.Autofac
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ComputedAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        // In seconds, MinValue means "use default"
        public double KeepAliveTime { get; set; } = double.MinValue;

        public ComputedAttribute() { }
        public ComputedAttribute(bool isEnabled) 
            => IsEnabled = isEnabled;
    }
}
