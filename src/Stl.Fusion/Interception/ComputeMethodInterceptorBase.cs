using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public abstract class ComputeMethodInterceptorBase : InterceptorBase
{
    public new record Options : InterceptorBase.Options
    {
        public IComputedOptionsProvider? ComputedOptionsProvider { get; init; }
    }

    public IComputedOptionsProvider ComputedOptionsProvider { get; }

    protected ComputeMethodInterceptorBase(Options options, IServiceProvider services)
        : base(options, services)
    {
        ComputedOptionsProvider = options.ComputedOptionsProvider
            ?? services.GetRequiredService<IComputedOptionsProvider>();
    }

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var computeMethodDef = (ComputeMethodDef) methodDef;
        var function = CreateFunction<T>(computeMethodDef);
        return invocation => {
            var input = computeMethodDef.CreateInput(function, invocation);
            var arguments = input.Arguments;
            var ctIndex = computeMethodDef.CancellationTokenArgumentIndex;
            var cancellationToken = ctIndex >= 0
                ? arguments.GetCancellationToken(ctIndex)
                : default;

            // Invoking the function
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

    protected override MethodDef? CreateMethodDef(MethodInfo methodInfo, Invocation initialInvocation)
    {
        ValidateType(initialInvocation.Proxy.GetType().NonProxyType());

        var proxyType = initialInvocation.Proxy.GetType();
        var options = ComputedOptionsProvider.GetComputedOptions(methodInfo, proxyType);
        if (options == null)
            return null;

        var methodDef = (ComputeMethodDef) Services.Activate(options.ComputeMethodDefType, this, methodInfo, proxyType);
        return methodDef.IsValid ? methodDef : null;
    }
}
