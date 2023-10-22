using StlErrors = Stl.Internal.Errors;

namespace Stl.Interception.Interceptors;

public class TypeFactoryInterceptor(IServiceProvider services) : Interceptor
{
    private static readonly Func<MethodInfo, ObjectFactory> CreateFactoryMethod = CreateFactory;
    // Factories are statically cached - Interceptor defines IServiceProvider scope
    private static readonly ConcurrentDictionary<MethodInfo, ObjectFactory> FactoriesCache = new();
    private readonly IServiceProvider _services = services;

    public override void Intercept(Invocation invocation)
        => throw StlErrors.NotSupported("TypeFactory doesn't support void methods.");

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        ObjectFactory m = FactoriesCache.GetOrAdd(invocation.Method, CreateFactoryMethod);
        // Probably not changeable.
        // ObjectFactory is `object ObjectFactory(IServiceProvider, object[] args)`.
        return (TResult)m(_services, invocation.Arguments.ToArray());
    }

    private static ObjectFactory CreateFactory(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var parameterTypes = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            parameterTypes[i] = parameters[i].ParameterType;
        } // bench: This is faster than any other option (Linq).

        return ActivatorUtilities.CreateFactory(method.ReturnType, parameterTypes);
    }
}
