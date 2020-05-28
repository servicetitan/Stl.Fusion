using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Castle.DynamicProxy;
using Stl.Fusion.Interception.Internal;

namespace Stl.Fusion.Interception
{
    public class InterceptedInput : ComputedInput, IEquatable<InterceptedInput>
    {
        public readonly InterceptedMethod Method;
        public readonly IInvocation Invocation;
        public readonly IInvocationProceedInfo ProceedInfo;
        // Shortcuts
        public object Target => Invocation.InvocationTarget;
        public object[] Arguments => Invocation.Arguments;
        public CancellationToken CancellationToken {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Method.CancellationTokenArgumentIndex >= 0
                ? (CancellationToken) Arguments[Method.CancellationTokenArgumentIndex]
                : default;
        }

        public InterceptedInput(IFunction function, InterceptedMethod method, IInvocation invocation)
            : base(function)
        {
            Method = method;
            Invocation = invocation;
            ProceedInfo = invocation.CaptureProceedInfo();

            var argumentComparers = method.ArgumentComparers;
            var hashCode = System.HashCode.Combine(
                HashCode,
                method.InvocationTargetComparer.GetHashCodeFunc(invocation.InvocationTarget));
            var arguments = Invocation.Arguments;
            for (var i = 0; i < arguments.Length; i++)
                hashCode ^= argumentComparers[i].GetHashCodeFunc(arguments[i]);
            HashCode = hashCode;
        }

        public override string ToString() 
            => $"{Function}({string.Join(", ", Arguments)})";

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
            if (!Method.InvocationTargetComparer.EqualsFunc(Target, other.Target))
                return false;
            var arguments = Arguments;
            var otherArguments = other.Arguments;
            // if (arguments == otherArguments)
            //     return true;
            if (arguments.Length != other.Arguments.Length)
                return false;
            var argumentComparers = Method.ArgumentComparers;
            // Backward direction is intended: tail arguments
            // are more likely to differ.
            for (var i = arguments.Length - 1; i >= 0; i--) {
                if (!argumentComparers[i].EqualsFunc(arguments[i], otherArguments[i]))
                    return false;
            }
            return true;
        }
        public override bool Equals(ComputedInput obj) 
            => obj is InterceptedInput other && Equals(other);
        public override bool Equals(object? obj) 
            => obj is InterceptedInput other && Equals(other);
        public override int GetHashCode() 
            => base.GetHashCode();
    }
}
