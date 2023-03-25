namespace Stl.Interception.Internal;

public static class Errors
{
    public static Exception NoProxyType(Type type)
        => new InvalidOperationException($"Type '{type.GetName()}' doesn't have a proxy type generated for it. Please verify that the package 'Stl.Generators' is installed.");

    public static Exception InvalidProxyType(Type? type, Type expectedType)
        => new InvalidOperationException(
            $"A proxy of type '{type?.GetName() ?? "null"}' is expected to implement '{expectedType.GetName()}'.");

    public static Exception InvalidInterceptedDelegate()
        => new InvalidOperationException("Invocation.InterceptedDelegate is null or doesn't have an expected type.");

    public static Exception NoProxyTarget()
        => new InvalidOperationException("Invocation.ProxyTarget is null.");

    // Proxy exceptions

    public static Exception NoInterceptor()
        => new InvalidOperationException("This proxy has no interceptor - you must call SetInterceptor method first.");

    public static Exception InterceptorIsAlreadyBound()
        => new InvalidOperationException("Interceptor is already bound.");
}
