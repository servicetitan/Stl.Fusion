using Castle.DynamicProxy;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public class ComputeMethodDef : MethodDef
{
    public ComputedOptions Options { get; protected init; } = ComputedOptions.Default;
    public ArgumentHandler InvocationTargetHandler { get; protected init; } = null!;
    public ArgumentHandler[] ArgumentHandlers { get; protected init; } = null!;
    public (ArgumentHandler Handler, int Index)[]? PreprocessingArgumentHandlers { get; protected init; }
    public int CancellationTokenArgumentIndex { get; protected init; } = -1;

    public ComputeMethodDef(
        ComputeMethodInterceptorBase interceptor,
        MethodInfo methodInfo)
        : base(interceptor, methodInfo)
    {
        if (!IsAsyncMethod)
            return;

        var options = interceptor.ComputedOptionsProvider.GetComputedOptions(methodInfo);
        if (options == null)
            return;

        Options = options;
        var invocationTargetType = methodInfo.ReflectedType;
        var parameters = methodInfo.GetParameters();
        var argumentHandlerProvider = interceptor.ArgumentHandlerProvider;
        InvocationTargetHandler = argumentHandlerProvider.GetInvocationTargetHandler(methodInfo, invocationTargetType!);
        ArgumentHandlers = new ArgumentHandler[parameters.Length];
        var preprocessingArgumentHandlers = new List<(ArgumentHandler Handler, int Index)>();
        for (var i = 0; i < parameters.Length; i++) {
            var p = parameters[i];
            var argumentHandler = argumentHandlerProvider.GetArgumentHandler(methodInfo, p);
            ArgumentHandlers[i] = argumentHandler;
            if (argumentHandler.PreprocessFunc != null)
                preprocessingArgumentHandlers.Add((argumentHandler, i));
            var parameterType = p.ParameterType;
            if (typeof(CancellationToken).IsAssignableFrom(parameterType))
                CancellationTokenArgumentIndex = i;
        }
        if (preprocessingArgumentHandlers.Count != 0)
            PreprocessingArgumentHandlers = preprocessingArgumentHandlers.ToArray();

        IsValid = true;
    }

    public virtual ComputeMethodInput CreateInput(IFunction function, AbstractInvocation invocation)
        => new(function, this, invocation);
}
