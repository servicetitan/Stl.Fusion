using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public abstract class ComputeMethodInterceptorBase : InterceptorBase
{
    public new record Options : InterceptorBase.Options;

    public ComputedOptionsProvider ComputedOptionsProvider { get; }

    protected ComputeMethodInterceptorBase(Options options, IServiceProvider services)
        : base(options, services)
        => ComputedOptionsProvider = services.GetRequiredService<ComputedOptionsProvider>();

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
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
            if (ctIndex >= 0)
                // We don't want memory leaks + unexpected cancellation later
                arguments.SetCancellationToken(ctIndex, default);

            // ReSharper disable once HeapView.BoxingAllocation
            return methodDef.ReturnsValueTask ? new ValueTask<T>(task) : task;
        };
    }

    protected abstract ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method);

    protected override MethodDef? CreateMethodDef(MethodInfo method, Invocation initialInvocation)
    {
        var type = initialInvocation.Proxy.GetType().NonProxyType();
        var options = ComputedOptionsProvider.GetComputedOptions(type, method);
        if (options == null)
            return null;

        var methodDef = new ComputeMethodDef(type, method, this);
        return methodDef.IsValid ? methodDef : null;
    }
}
