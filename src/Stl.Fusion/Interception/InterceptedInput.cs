using System;
using System.Threading;
using Castle.DynamicProxy;

namespace Stl.Fusion.Interception
{
    public class InterceptedInput : ComputedInput, IEquatable<InterceptedInput>
    {
        // ReSharper disable once HeapView.BoxingAllocation
        private static readonly object BoxedDefaultCancellationToken = (CancellationToken) default;

        public readonly InterceptedMethodDescriptor Method;
        public readonly IInvocation Invocation;
        public readonly IInvocationProceedInfo ProceedInfo;
        // Shortcuts
        public object Target => Invocation.InvocationTarget;
        public object[] Arguments => Invocation.Arguments;

        public InterceptedInput(IFunction function, InterceptedMethodDescriptor method, IInvocation invocation)
            : base(function)
        {
            Method = method;
            Invocation = invocation;
            ProceedInfo = invocation.CaptureProceedInfo();

            var arguments = invocation.Arguments;
            var argumentHandlers = method.ArgumentHandlers;
            var preprocessingArgumentHandlers = method.PreprocessingArgumentHandlers;
            if (preprocessingArgumentHandlers != null) {
                foreach (var (handler, index) in preprocessingArgumentHandlers)
                    handler.PreprocessFunc!.Invoke(method, invocation, index);
            }

            var hashCode = System.HashCode.Combine(
                HashCode,
                method.InvocationTargetHandler.GetHashCodeFunc(invocation.InvocationTarget));
            for (var i = 0; i < arguments.Length; i++)
                hashCode ^= argumentHandlers[i].GetHashCodeFunc(arguments[i]);
            HashCode = hashCode;
        }

        public override string ToString()
            => $"{Function}({string.Join(", ", Arguments)})";

        public object InvokeOriginalFunction(CancellationToken cancellationToken)
        {
            // This method fixes up the arguments before the invocation so that
            // CancellationToken is set to the correct one and CallOptions are reset.
            // In addition, it processes CallOptions.Capture, though note that
            // it's also processed in InterceptedFunction.TryGetExisting.

            var method = Method;
            var arguments = Arguments;
            var ctIndex = method.CancellationTokenArgumentIndex;
            if (ctIndex >= 0) {
                var currentCancellationToken = (CancellationToken) arguments[ctIndex];
                // Comparison w/ the existing one to avoid boxing when possible
                if (currentCancellationToken != cancellationToken) {
                    // ReSharper disable once HeapView.BoxingAllocation
                    arguments[ctIndex] = cancellationToken;
                    ProceedInfo.Invoke();
                    arguments[ctIndex] = BoxedDefaultCancellationToken;
                }
                else
                    ProceedInfo.Invoke();
            }
            else
                ProceedInfo.Invoke();

            return Invocation.ReturnValue;
        }

        // Equality

        public bool Equals(InterceptedInput? other)
        {
            if (other == null)
                return false;
            if (HashCode != other.HashCode)
                return false;
            if (!ReferenceEquals(Method, other.Method))
                return false;
            // GetType() & other.GetType() are the same here, because
            // Method & other.Method are the same

            var arguments = Arguments;
            var otherArguments = other.Arguments;
            var argumentHandlers = Method.ArgumentHandlers;
            // Backward direction is intended: tail arguments
            // are more likely to differ.
            for (var i = arguments.Length - 1; i >= 0; i--) {
                if (!argumentHandlers[i].EqualsFunc(arguments[i], otherArguments[i]))
                    return false;
            }
            if (!Method.InvocationTargetHandler.EqualsFunc(Target, other.Target))
                return false;
            return true;
        }
        public override bool Equals(ComputedInput? obj)
            => obj is InterceptedInput other && Equals(other);
        public override bool Equals(object? obj)
            => obj is InterceptedInput other && Equals(other);
        public override int GetHashCode()
            => base.GetHashCode();
    }
}
