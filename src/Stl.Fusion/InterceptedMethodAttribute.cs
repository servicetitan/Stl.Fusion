using System;

namespace Stl.Fusion
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public abstract class InterceptedMethodAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public Type? InterceptedMethodDescriptorType { get; set; } = null;

        protected InterceptedMethodAttribute() { }
        protected InterceptedMethodAttribute(bool isEnabled)
            => IsEnabled = isEnabled;
    }
}
