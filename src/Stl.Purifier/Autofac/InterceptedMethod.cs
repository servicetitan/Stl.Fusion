using System;
using System.Reflection;

namespace Stl.Purifier.Autofac
{
    public class InterceptedMethod
    {
        public MethodInfo MethodInfo { get; set; } = null!;
        public Type OutputType { get; set; } = null!;
        public bool ReturnsValueTask { get; set; }
        public bool ReturnsComputed { get; set; }
        public ArgumentComparer InvocationTargetComparer { get; set; } = null!;
        public ArgumentComparer[] ArgumentComparers { get; set; } = null!;
        public int CancellationTokenArgumentIndex { get; set; } = -1;
        public int? KeepAliveTime { get; set; }
    }
}
