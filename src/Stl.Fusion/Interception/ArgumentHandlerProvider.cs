using Stl.Extensibility;

namespace Stl.Fusion.Interception;

public interface IArgumentHandlerProvider
{
    ArgumentHandler GetInvocationTargetHandler(MethodInfo methodInfo, Type invocationTargetType);
    ArgumentHandler GetArgumentHandler(MethodInfo methodInfo, ParameterInfo parameterInfo);
}

public class ArgumentHandlerProvider : IArgumentHandlerProvider, IHasServices
{
    public IServiceProvider Services { get; }
    public IMatchingTypeFinder MatchingTypeFinder { get; }

    static ArgumentHandlerProvider()
    {
        // Let's register this assembly in the list of assemblies
        // MatchingTypeFinder scans by default.
        Extensibility.MatchingTypeFinder.AddAssembly(typeof(ArgumentHandlerProvider).Assembly);
    }

    public ArgumentHandlerProvider(IServiceProvider services)
    {
        Services = services;
        MatchingTypeFinder = services.GetRequiredService<IMatchingTypeFinder>();
    }

    public ArgumentHandler GetInvocationTargetHandler(MethodInfo methodInfo, Type invocationTargetType)
        => GetArgumentHandler(invocationTargetType, true);

    public virtual ArgumentHandler GetArgumentHandler(MethodInfo methodInfo, ParameterInfo parameterInfo)
        => GetArgumentHandler(parameterInfo.ParameterType);

    protected virtual ArgumentHandler GetArgumentHandler(Type type, bool isInvocationTarget = false)
    {
        var handlerType = MatchingTypeFinder.TryFind(type, typeof(ArgumentHandlerProvider));
        if (handlerType != null)
            return CreateHandler(handlerType);

        if (isInvocationTarget)
            return ByRefArgumentHandler.Instance;
        var equatableType = typeof(IEquatable<>).MakeGenericType(type);
        if (equatableType.IsAssignableFrom(type)) {
            var eacType = typeof(EquatableArgumentHandler<>).MakeGenericType(type);
            return CreateHandler(eacType);
        }
        return ArgumentHandler.Default;
    }

    protected virtual ArgumentHandler CreateHandler(Type handlerType)
    {
        var pInstance = handlerType.GetProperty(
            nameof(ByRefArgumentHandler.Instance),
            BindingFlags.Static | BindingFlags.Public);
        if (pInstance != null)
            return (ArgumentHandler) pInstance!.GetValue(null)!;

        var fInstance = handlerType.GetField(
            nameof(ByRefArgumentHandler.Instance),
            BindingFlags.Static | BindingFlags.Public);
        if (fInstance != null)
            return (ArgumentHandler) fInstance!.GetValue(null)!;

        return (ArgumentHandler) Services.Activate(handlerType);
    }
}
