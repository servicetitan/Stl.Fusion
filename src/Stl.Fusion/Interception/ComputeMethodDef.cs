using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public sealed record ComputeMethodDef : MethodDef
{
    public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;

    public ComputeMethodDef(
        Type type,
        MethodInfo method,
        ComputeMethodInterceptorBase interceptor)
        : base(type, method)
    {
        if (!IsAsyncMethod) {
            IsValid = false;
            return;
        }

        var computedOptions = interceptor.ComputedOptionsProvider.GetComputedOptions(type, method);
        if (computedOptions == null) {
            IsValid = false;
            return;
        }

        ComputedOptions = computedOptions;
    }

    public ComputeMethodInput CreateInput(IFunction function, Invocation invocation)
        => new(function, this, invocation);

    // All XxxMethodDef records should rely on reference-based equality
    public bool Equals(ComputeMethodDef? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
