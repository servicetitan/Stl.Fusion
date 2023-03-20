using Castle.DynamicProxy;
using Stl.Interception;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public abstract class ComputeMethodInterceptorBase : InterceptorBase
{
    // ReSharper disable once HeapView.BoxingAllocation
    private static readonly object NoCancellationTokenBoxed = CancellationToken.None;

    public new record Options : InterceptorBase.Options
    {
        public IComputedOptionsProvider? ComputedOptionsProvider { get; init; }
        public IArgumentHandlerProvider? ArgumentHandlerProvider { get; init; }
    }

    public IComputedOptionsProvider ComputedOptionsProvider { get; }
    public IArgumentHandlerProvider ArgumentHandlerProvider { get; }

    protected ComputeMethodInterceptorBase(Options options, IServiceProvider services)
        : base(options, services)
    {
        ComputedOptionsProvider = options.ComputedOptionsProvider
            ?? services.GetRequiredService<IComputedOptionsProvider>();
        ArgumentHandlerProvider = options.ArgumentHandlerProvider
            ?? services.GetRequiredService<IArgumentHandlerProvider>();
    }

    protected override Func<Invocation, object?> CreateHandler<T>(Invocation initialInvocation, MethodDef methodDef)
    {
        var computeMethodDef = (ComputeMethodDef) methodDef;
        var function = CreateFunction<T>(computeMethodDef);
        return invocation => {
            var input = computeMethodDef.CreateInput(function, (AbstractInvocation) invocation);
            var arguments = input.Arguments;
            var cancellationTokenIndex = computeMethodDef.CancellationTokenArgumentIndex;
            var cancellationToken = cancellationTokenIndex >= 0
                ? (CancellationToken) arguments[cancellationTokenIndex]
                : default;

            // Invoking the function
            var usedBy = Computed.GetCurrent();

            // InvokeAndStrip allows to get rid of one extra allocation
            // of a task stripping the result of regular Invoke.
            var task = function.InvokeAndStrip(input, usedBy, null, cancellationToken);
            if (cancellationTokenIndex >= 0)
                // We don't want memory leaks + unexpected cancellation later
                arguments[cancellationTokenIndex] = NoCancellationTokenBoxed;

            if (methodDef.ReturnsValueTask)
                // ReSharper disable once HeapView.BoxingAllocation
                invocation.ReturnValue = new ValueTask<T>(task);
            else
                invocation.ReturnValue = task;
        };
    }

    protected abstract ComputeFunctionBase<T> CreateFunction<T>(ComputeMethodDef method);

    protected override MethodDef? CreateMethodDef(MethodInfo methodInfo, IInvocation initialInvocation)
    {
        ValidateType(initialInvocation.TargetType);

        var proxyType = initialInvocation.Proxy.GetType();
        var options = ComputedOptionsProvider.GetComputedOptions(methodInfo, proxyType);
        if (options == null)
            return null;

        var methodDef = (ComputeMethodDef) Services.Activate(options.ComputeMethodDefType, this, methodInfo, proxyType);
        return methodDef.IsValid ? methodDef : null;
    }
}
