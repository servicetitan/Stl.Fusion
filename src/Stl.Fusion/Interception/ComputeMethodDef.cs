using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public sealed record ComputeMethodDef : MethodDef
{
    public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;
    public int CancellationTokenArgumentIndex { get; init; } = -1;

    public ComputeMethodDef(
        Type type,
        MethodInfo method,
        ComputeMethodInterceptorBase interceptor)
        : base(type, method, interceptor)
    {
        if (!IsAsyncMethod)
            return;

        var computedOptions = interceptor.ComputedOptionsProvider.GetComputedOptions(type, method);
        if (computedOptions == null)
            return;

        ComputedOptions = computedOptions;
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++) {
            var p = parameters[i];
            if (typeof(CancellationToken).IsAssignableFrom(p.ParameterType))
                CancellationTokenArgumentIndex = i;
        }
        IsValid = true;
    }

    public override MethodDef ToReplicaMethodDef()
        => this;

    public ComputeMethodInput CreateInput(IFunction function, Invocation invocation)
        => new(function, this, invocation);

    // All XxxMethodDef records should rely on reference-based equality
    public bool Equals(ComputeMethodDef? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
