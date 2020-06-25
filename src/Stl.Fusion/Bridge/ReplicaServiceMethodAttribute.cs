using System;

namespace Stl.Fusion.Bridge
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplicaServiceMethodAttribute : InterceptedMethodAttribute
    {
        public ReplicaServiceMethodAttribute() { }
        public ReplicaServiceMethodAttribute(bool isEnabled) : base(isEnabled) { }
    }
}
