using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public sealed class ComputeMethodDef : MethodDef
{
    public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;
    public readonly bool IsDisposable;

    public ComputeMethodDef(
        Type type,
        MethodInfo method,
        ComputeServiceInterceptorBase interceptor)
        : base(type, method)
    {
        if (!IsAsyncMethod) {
            IsValid = false;
            return;
        }

        var computedOptions = interceptor.Hub.ComputedOptionsProvider.GetComputedOptions(type, method);
        if (computedOptions == null) {
            IsValid = false;
            return;
        }

        IsDisposable = typeof(IHasIsDisposed).IsAssignableFrom(type);
        ComputedOptions = computedOptions;
    }

    public ComputeMethodInput CreateInput(IFunction function, Invocation invocation)
        => new(function, this, invocation);
}
