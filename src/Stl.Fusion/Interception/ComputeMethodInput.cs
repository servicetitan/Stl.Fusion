using Cysharp.Text;
using Stl.Interception;

namespace Stl.Fusion.Interception;

public sealed class ComputeMethodInput : ComputedInput, IEquatable<ComputeMethodInput>
{
    public readonly ComputeMethodDef MethodDef;
    public readonly Invocation Invocation;
    // Shortcuts
    public object Service => Invocation.Proxy;
    public ArgumentList Arguments => Invocation.Arguments;

    public ComputeMethodInput(IFunction function, ComputeMethodDef methodDef, Invocation invocation)
    {
        MethodDef = methodDef;
        Invocation = invocation;

        var arguments = invocation.Arguments;
        var hashCode = unchecked(
            arguments.GetHashCode(methodDef.CancellationTokenArgumentIndex) +
            367*invocation.Proxy.GetHashCode() +
            7817*HashCode);
        Initialize(function, hashCode);
    }

    public override string ToString()
        => ZString.Concat(Category, "(", ZString.Join(", ", Arguments), ") #", HashCode);

    public object InvokeOriginalFunction(CancellationToken cancellationToken)
    {
        // This method fixes up the arguments before the invocation so that
        // CancellationToken is set to the correct one and CallOptions are reset.
        // In addition, it processes CallOptions.Capture, though note that
        // it's also processed in InterceptedFunction.TryGetExisting.
        var ctIndex = MethodDef.CancellationTokenArgumentIndex;
        if (ctIndex < 0)
            return Invocation.InterceptedUntyped()!;

        Arguments.SetItem(ctIndex, cancellationToken);
        var result = Invocation.InterceptedUntyped()!;
        Arguments.SetItem(ctIndex, default(CancellationToken));
        return result;
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

        return Arguments.Equals(other.Arguments, methodDef.CancellationTokenArgumentIndex);
    }
    public override bool Equals(ComputedInput? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override bool Equals(object? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override int GetHashCode()
        => base.GetHashCode();
}
