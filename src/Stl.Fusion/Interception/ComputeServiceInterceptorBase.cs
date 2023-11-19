using System.Diagnostics.CodeAnalysis;
using Stl.Fusion.Internal;
using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public abstract class ComputeServiceInterceptorBase(
        ComputeServiceInterceptorBase.Options settings,
        IServiceProvider services
        ) : InterceptorBase(settings, services)
{
    public new record Options : InterceptorBase.Options;

    public readonly FusionInternalHub Hub = services.GetRequiredService<FusionInternalHub>();

    public override void Intercept(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? Hub.CommandServiceInterceptor.GetHandler(invocation);
        if (handler == null)
            invocation.Intercepted();
        else
            handler(invocation);
    }

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        var handler = GetHandler(invocation) ?? Hub.CommandServiceInterceptor.GetHandler(invocation);
        return handler == null
            ? invocation.Intercepted<TResult>()
            : (TResult)handler.Invoke(invocation)!;
    }

    protected override Func<Invocation, object?> CreateHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (Invocation initialInvocation, MethodDef methodDef)
    {
        var computeMethodDef = (ComputeMethodDef)methodDef;
        var function = CreateFunction<T>(computeMethodDef);
        return invocation => {
            var input = computeMethodDef.CreateInput(function, invocation);
            var arguments = input.Arguments;
            var ctIndex = computeMethodDef.CancellationTokenIndex;
            var cancellationToken = ctIndex >= 0
                ? arguments.GetCancellationToken(ctIndex)
                : default;
            var usedBy = Computed.GetCurrent();

            // InvokeAndStrip allows to get rid of one extra allocation
            // of a task stripping the result of regular Invoke.
            var task = function.InvokeAndStrip(input, usedBy, null, cancellationToken);
            if (cancellationToken != default)
                // We don't want memory leaks + unexpected cancellation later
                arguments.SetCancellationToken(ctIndex, default);

            // ReSharper disable once HeapView.BoxingAllocation
            return computeMethodDef.ReturnsValueTask ? new ValueTask<T>(task) : task;
        };
    }

    protected abstract ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method);

    // We don't need to decorate this method with any dynamic access attributes
    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
    {
        var type = initialInvocation.Proxy.GetType().NonProxyType();
#pragma warning disable IL2072
        var options = Hub.ComputedOptionsProvider.GetComputedOptions(type, method);
        if (options == null)
            return null;

        var methodDef = new ComputeMethodDef(type, method, this);
#pragma warning restore IL2072
        return methodDef.IsValid ? methodDef : null;
    }

    protected override void ValidateTypeInternal(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        => Hub.CommandServiceInterceptor.ValidateType(type);
}
