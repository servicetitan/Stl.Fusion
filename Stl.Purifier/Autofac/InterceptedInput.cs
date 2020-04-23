using System;
using System.Threading;
using Castle.DynamicProxy;

namespace Stl.Purifier.Autofac
{
    public readonly struct InterceptedInput : IEquatable<InterceptedInput>
    {
        public readonly InterceptedMethod Method;
        public readonly IInvocation Invocation;
        public readonly IInvocationProceedInfo ProceedInfo;
        public readonly int HashCode;
        // Shortcuts
        public object Target => Invocation.InvocationTarget;
        public object[] Arguments => Invocation.Arguments;
        public CancellationToken CancellationToken =>
            Method.CancellationTokenArgumentIndex >= 0
                ? (CancellationToken) Arguments[Method.CancellationTokenArgumentIndex]
                : default;

        public InterceptedInput(InterceptedMethod method, IInvocation invocation)
        {
            Method = method;
            Invocation = invocation;
            ProceedInfo = invocation.CaptureProceedInfo();

            var argumentComparers = method.ArgumentComparers;
            var hashCode = method.InvocationTargetComparer.GetHashCode(invocation.InvocationTarget);
            var arguments = Invocation.Arguments;
            for (var i = 0; i < arguments.Length; i++) {
                unchecked {
                    hashCode = hashCode * 347 + argumentComparers[i].GetHashCode(arguments[i]);
                }
            }
            HashCode = hashCode;
        }

        public override string ToString() => $"[{string.Join(", ", Arguments)}]";

        public object InvokeOriginalFunction(IComputed computed, CancellationToken cancellationToken)
        {
            // This method fixes up the arguments before the invocation so that
            // CancellationToken is set to the correct one and CallOptions are reset.
            // In addition, it processes CallOptions.Capture, though note that
            // it's also processed in InterceptedFunction.TryGetCached.

            var method = Method;
            var arguments = Arguments;
            if (method.CancellationTokenArgumentIndex >= 0) {
                var currentCancellationToken = (CancellationToken) arguments[method.CancellationTokenArgumentIndex];
                // Comparison w/ the existing one to avoid boxing when possible
                if (currentCancellationToken != cancellationToken)
                    // ReSharper disable once HeapView.BoxingAllocation
                    arguments[method.CancellationTokenArgumentIndex] = cancellationToken;
            }

            ProceedInfo.Invoke();
            return Invocation.ReturnValue;
        }


        // Equality

        public bool Equals(InterceptedInput other)
        {
            if (HashCode != other.HashCode)
                return false;
            if (!Method.InvocationTargetComparer.Equals(Target, other.Target))
                return false;
            var arguments = Arguments;
            var otherArguments = other.Arguments;
            // if (arguments == otherArguments)
            //     return true;
            if (arguments.Length != other.Arguments.Length)
                return false;
            var argumentComparers = Method.ArgumentComparers;
            for (var i = 0; i < arguments.Length; i++) {
                if (!argumentComparers[i].Equals(arguments[i], otherArguments[i]))
                    return false;
            }
            return true;
        }
        public override bool Equals(object? obj) => obj is InterceptedInput other && Equals(other);
        public override int GetHashCode() => HashCode;
        public static bool operator ==(InterceptedInput left, InterceptedInput right) => left.Equals(right);
        public static bool operator !=(InterceptedInput left, InterceptedInput right) => !left.Equals(right);
    }
}
