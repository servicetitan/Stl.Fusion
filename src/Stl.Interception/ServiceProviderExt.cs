namespace Stl.Interception;

public static class ServiceProviderExt
{
    // ActivateProxy

    public static TType ActivateProxy<TType>(
        this IServiceProvider services,
        Interceptor interceptor, TType? proxyTarget = null)
        where TType : class
    {
        var proxyType = Proxies.GetProxyType<TType>();
        var proxy = (TType)services.Activate(proxyType);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy ActivateProxy(
        this IServiceProvider services,
        Type type, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxyType = Proxies.GetProxyType(type);
        var proxy = (IProxy)services.Activate(proxyType);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    // GetTypeViewFactory

    public static ITypeViewFactory TypeViewFactory(this IServiceProvider services)
        => services.GetService<ITypeViewFactory>() ?? Interception.TypeViewFactory.Default;

    public static TypeViewFactory<TView> TypeViewFactory<TView>(this IServiceProvider services)
        where TView : class
        => services.TypeViewFactory().For<TView>();
}
