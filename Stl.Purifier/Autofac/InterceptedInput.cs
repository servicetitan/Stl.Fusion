using System;
using Castle.DynamicProxy;

namespace Stl.Purifier.Autofac
{
    public readonly struct InterceptedInput : IEquatable<InterceptedInput>
    {
        public readonly object Target;
        public readonly object[] Arguments;
        public readonly int UsedArgumentBitmap;
        public readonly IInvocationProceedInfo ProceedInfo;
        public readonly int HashCode;

        public InterceptedInput(object target, object[] arguments, int usedArgumentBitmap, IInvocationProceedInfo proceedInfo)
        {
            if (arguments.Length > 30)
                throw new ArgumentOutOfRangeException(nameof(arguments));
            Target = target;
            Arguments = arguments;
            UsedArgumentBitmap = usedArgumentBitmap;
            ProceedInfo = proceedInfo;

            var hashCode = Target.GetHashCode();
            for (var i = 0; i < arguments.Length; i++, usedArgumentBitmap >>= 1) {
                if ((usedArgumentBitmap & 1) == 0)
                    continue;
                var item = arguments[i];
                unchecked {
                    hashCode = hashCode * 347 + (item?.GetHashCode() ?? 0);
                }
            }
            HashCode = hashCode;
        }

        public override string ToString() => $"[{string.Join(", ", Arguments)}]";

        public bool Equals(InterceptedInput other)
        {
            if (HashCode != other.HashCode)
                return false;
            if (!ReferenceEquals(Target, other.Target))
                return false;
            var otherItems = other.Arguments;
            if (ReferenceEquals(Arguments, otherItems))
                return true;
            if (Arguments.Length != otherItems.Length)
                return false;
            var usedArgumentBitmap = UsedArgumentBitmap;
            for (var i = 0; i < Arguments.Length; i++, usedArgumentBitmap >>= 1) {
                if ((usedArgumentBitmap & 1) == 0)
                    continue;
                if (!Equals(Arguments[i], otherItems[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object? obj) 
            => obj is InterceptedInput other && Equals(other);
        public override int GetHashCode() => HashCode;
        public static bool operator ==(InterceptedInput left, InterceptedInput right) => left.Equals(right);
        public static bool operator !=(InterceptedInput left, InterceptedInput right) => !left.Equals(right);
    }
}
