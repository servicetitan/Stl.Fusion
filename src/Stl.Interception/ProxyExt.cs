namespace Stl.Interception;

public static class ProxyExt
{
    public static IServiceProvider GetServices(object proxy)
    {
        var interceptor = ((IProxy)proxy).Interceptor;
        return ((IHasServices)interceptor).Services;
    }
}
