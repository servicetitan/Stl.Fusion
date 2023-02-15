using Castle.DynamicProxy;
using Stl.Fusion.Swapping;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public record ComputeMethodDef : MethodDef
{
    public ComputedOptions ComputedOptions { get; init; } = ComputedOptions.Default;
    public ArgumentHandler InvocationTargetHandler { get; init; } = null!;
    public ArgumentHandler[] ArgumentHandlers { get; init; } = null!;
    public (ArgumentHandler Handler, int Index)[]? PreprocessingArgumentHandlers { get; init; }
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

    public override MethodDef ToReplicaMethodDef() =>
        ReferenceEquals(ComputedOptions.SwappingOptions, SwappingOptions.NoSwapping)
            ? this
            : this with {
                ComputedOptions = ComputedOptions with {
                    SwappingOptions = SwappingOptions.NoSwapping
                }
            };

    public virtual ComputeMethodInput CreateInput(IFunction function, AbstractInvocation invocation)
        => new(function, this, invocation);
}
