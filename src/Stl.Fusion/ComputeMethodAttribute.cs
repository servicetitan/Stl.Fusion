using System;

namespace Stl.Fusion
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ComputeMethodAttribute : Attribute
    {
        public bool IsEnabled { get; } = true;
        public bool RewriteErrors { get; set; }
        public Type? ComputeMethodDefType { get; set; } = null;
        // In seconds, NaN means "use default"
        public double KeepAliveTime { get; set; } = Double.NaN;
        public double ErrorAutoInvalidateTime { get; set; } = Double.NaN;
        public double AutoInvalidateTime { get; set; } = Double.NaN;

        public ComputeMethodAttribute() { }
        public ComputeMethodAttribute(bool isEnabled) => IsEnabled = isEnabled;
    }
}
