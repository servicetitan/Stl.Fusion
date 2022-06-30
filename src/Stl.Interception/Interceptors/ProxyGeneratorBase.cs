using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public abstract class ProxyGeneratorBase
{
    public static DefaultProxyBuilder ProxyBuilder { get; } = new();
}
