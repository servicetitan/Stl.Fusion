using Castle.DynamicProxy;
using Cysharp.Text;
using Stl.Fusion.Interception.Internal;

namespace Stl.Fusion.Interception;

public sealed class ComputeMethodInput : ComputedInput, IEquatable<ComputeMethodInput>
{
    // ReSharper disable once HeapView.BoxingAllocation
    private static readonly object BoxedDefaultCancellationToken = default(CancellationToken);

    public readonly ComputeMethodDef MethodDef;
    public readonly AbstractInvocation Invocation;
    public readonly int NextInterceptorIndex;
    // Shortcuts
    public object Target => Invocation.InvocationTarget;
    public object[] Arguments => Invocation.Arguments;

    public ComputeMethodInput(IFunction function, ComputeMethodDef methodDef, AbstractInvocation invocation)
    {
        MethodDef = methodDef;
        Invocation = invocation;
        NextInterceptorIndex = invocation.GetCurrentInterceptorIndex();

        var arguments = invocation.Arguments;
        var argumentHandlers = methodDef.ArgumentHandlers;
        var preprocessingArgumentHandlers = methodDef.PreprocessingArgumentHandlers;
        if (preprocessingArgumentHandlers != null) {
            foreach (var (handler, index) in preprocessingArgumentHandlers)
                handler.PreprocessFunc!.Invoke(methodDef, invocation, index);
        }

        var hashCode = System.HashCode.Combine(
            HashCode,
            methodDef.InvocationTargetHandler.GetHashCodeFunc(invocation.InvocationTarget));
        for (var i = 0; i < arguments.Length; i++)
            hashCode ^= argumentHandlers[i].GetHashCodeFunc(arguments[i]);
        Initialize(function, hashCode);
    }

    public override string ToString()
        => ZString.Concat(Category, "(", ZString.Join(", ", Arguments), ") #", HashCode);

    public override IComputed? GetExistingComputed()
        => ComputedRegistry.Instance.Get(this);

    public object InvokeOriginalFunction(CancellationToken cancellationToken)
    {
        // This method fixes up the arguments before the invocation so that
        // CancellationToken is set to the correct one and CallOptions are reset.
        // In addition, it processes CallOptions.Capture.

        var methodDef = MethodDef;
        var arguments = Arguments;
        var ctIndex = methodDef.CancellationTokenArgumentIndex;
        Invocation.SetCurrentInterceptorIndex(NextInterceptorIndex);
        if (ctIndex >= 0) {
            var currentCancellationToken = (CancellationToken) arguments[ctIndex];
            // Comparison w/ the existing one to avoid boxing when possible
            if (currentCancellationToken != cancellationToken) {
                // ReSharper disable once HeapView.BoxingAllocation
                arguments[ctIndex] = cancellationToken;
                Invocation.Proceed();
                arguments[ctIndex] = BoxedDefaultCancellationToken;
            }
            else
                Invocation.Proceed();
        }
        else
            Invocation.Proceed();

        return Invocation.ReturnValue;
    }

    // Equality

    public bool Equals(ComputeMethodInput? other)
    {
        if (other == null)
            return false;
        if (HashCode != other.HashCode)
            return false;
        var methodDef = MethodDef;
        if (!ReferenceEquals(methodDef, other.MethodDef))
            return false;
        // GetType() & other.GetType() are the same here, because
        // Method & other.Method are the same

        var arguments = Arguments;
        var otherArguments = other.Arguments;
        var argumentHandlers = methodDef.ArgumentHandlers;
        // Backward direction is intended: tail arguments
        // are more likely to differ.
        for (var i = arguments.Length - 1; i >= 0; i--) {
            if (!argumentHandlers[i].EqualsFunc(arguments[i], otherArguments[i]))
                return false;
        }
        if (!methodDef.InvocationTargetHandler.EqualsFunc(Target, other.Target))
            return false;
        return true;
    }
    public override bool Equals(ComputedInput? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override bool Equals(object? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override int GetHashCode()
        => base.GetHashCode();
}
