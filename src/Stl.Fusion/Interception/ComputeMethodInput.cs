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
        var hashCode = methodDef.GetHashCode()
            ^ invocation.Proxy.GetHashCode()
            ^ arguments.GetHashCode(methodDef.CancellationTokenIndex);
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
        var ctIndex = MethodDef.CancellationTokenIndex;
        if (ctIndex < 0)
            return Invocation.InterceptedUntyped()!;

        Arguments.SetCancellationToken(ctIndex, cancellationToken);
        var result = Invocation.InterceptedUntyped()!;
        Arguments.SetCancellationToken(ctIndex, default);
        return result;
    }

    // Equality

    public bool Equals(ComputeMethodInput? other)
        => other != null
        && HashCode == other.HashCode
        && ReferenceEquals(MethodDef, other.MethodDef)
        && ReferenceEquals(Service, other.Service)
        && Arguments.Equals(other.Arguments, MethodDef.CancellationTokenIndex);

    public override bool Equals(ComputedInput? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override bool Equals(object? obj)
        => obj is ComputeMethodInput other && Equals(other);
    public override int GetHashCode()
        => HashCode;
}
