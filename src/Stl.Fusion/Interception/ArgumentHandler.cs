using System;
using Castle.DynamicProxy;

namespace Stl.Fusion.Interception
{
    public class ArgumentHandler
    {
        public static ArgumentHandler Default { get; } = new();

        public Func<object?, int> GetHashCodeFunc { get; protected set; } =
            o => o?.GetHashCode() ?? 0;
        public Func<object?, object?, bool> EqualsFunc { get; protected set; } =
            (objA, objB) => objA == objB || (objA?.Equals(objB) ?? false);
        public Func<object?, string> ToStringFunc { get; protected set; } =
            o => o?.ToString() ?? "␀";
        public Action<InterceptedMethodDescriptor, IInvocation, int>? PreprocessFunc { get; protected set; } = null;
    }
}
