using Stl.Internal;

namespace Stl.Interception.Interceptors;

public class TypedFactoryInterceptor(IServiceProvider services) : Interceptor
{
    private static readonly Func<MethodInfo, ObjectFactory> CreateFactoryMethod = CreateFactory;
    private static readonly ConcurrentDictionary<MethodInfo, ObjectFactory> ObjectFactoryCache = new();

    public override void Intercept(Invocation invocation)
        => throw Errors.NotSupported($"{nameof(TypedFactoryInterceptor)} doesn't support void methods.");

    public override TResult Intercept<TResult>(Invocation invocation)
    {
        // ObjectFactory is `object ObjectFactory(IServiceProvider, object[] args)`.
        var factory = ObjectFactoryCache.GetOrAdd(invocation.Method, CreateFactoryMethod);
        return (TResult)factory(services, invocation.Arguments.ToArray());
    }

    private static ObjectFactory CreateFactory(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var parameterTypes = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
            parameterTypes[i] = parameters[i].ParameterType;
#pragma warning disable IL2072
        return ActivatorUtilities.CreateFactory(method.ReturnType, parameterTypes);
#pragma warning restore IL2072
    }
}
