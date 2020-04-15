using System;

namespace Stl.Purifier.Autofac
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ComputedAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;

        public ComputedAttribute() { }
        public ComputedAttribute(bool isEnabled) 
            => IsEnabled = isEnabled;
    }
}
