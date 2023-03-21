using Stl.Fusion.Swapping;
using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public record ComputeMethodDef : MethodDef
{
    public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;
    public int CancellationTokenArgumentIndex { get; init; } = -1;

    public ComputeMethodDef(
        ComputeMethodInterceptorBase interceptor,
        MethodInfo methodInfo,
        Type proxyType)
        : base(interceptor, methodInfo)
    {
        if (!IsAsyncMethod)
            return;

        var computedOptions = interceptor.ComputedOptionsProvider.GetComputedOptions(methodInfo, proxyType);
        if (computedOptions == null)
            return;

        ComputedOptions = computedOptions;
        var parameters = methodInfo.GetParameters();
        for (var i = 0; i < parameters.Length; i++) {
            var p = parameters[i];
            if (typeof(CancellationToken).IsAssignableFrom(p.ParameterType))
                CancellationTokenArgumentIndex = i;
        }
        IsValid = true;
    }

    public override MethodDef ToReplicaMethodDef() =>
        ReferenceEquals(ComputedOptions.SwappingOptions, SwappingOptions.NoSwapping)
            ? this
            : this with {
                ComputedOptions = ComputedOptions with {
                    SwappingOptions = SwappingOptions.NoSwapping
                }
            };

    public virtual ComputeMethodInput CreateInput(IFunction function, Invocation invocation)
        => new(function, this, invocation);
}
