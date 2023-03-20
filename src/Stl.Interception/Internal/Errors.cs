namespace Stl.Interception.Internal;

public static class Errors
{
    public static Exception NoProxyType(Type type)
        => new InvalidOperationException($"Type {type.GetName()} doesn't have a proxy type generated for it.");

    public static Exception InvalidInterceptedDelegate()
        => new InvalidOperationException("Invocation.InterceptedDelegate is null or doesn't have an expected type.");

    public static Exception NoProxyTarget()
        => new InvalidOperationException("Invocation.ProxyTarget is null.");
}
