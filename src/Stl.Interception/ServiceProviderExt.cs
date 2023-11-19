using System.Diagnostics.CodeAnalysis;

namespace Stl.Interception;

public static class ServiceProviderExt
{
    // ActivateProxy

    public static TType ActivateProxy<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TType>(
        this IServiceProvider services,
        Interceptor interceptor, TType? proxyTarget = null)
        where TType : class, IRequiresAsyncProxy
    {
        var proxyType = Proxies.GetProxyType<TType>();
#pragma warning disable IL2072
        var proxy = (TType)services.Activate(proxyType);
#pragma warning restore IL2072
        return interceptor.BindTo(proxy, proxyTarget);
    }

    public static IProxy ActivateProxy(
        this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        Interceptor interceptor, object? proxyTarget = null)
    {
        var proxyType = Proxies.GetProxyType(type);
#pragma warning disable IL2072
        var proxy = (IProxy)services.Activate(proxyType);
#pragma warning restore IL2072
        return InterceptorExt.BindTo(interceptor, proxy, proxyTarget);
    }

    // GetTypeViewFactory

    public static ITypeViewFactory TypeViewFactory(this IServiceProvider services)
        => services.GetService<ITypeViewFactory>() ?? Interception.TypeViewFactory.Default;

    public static TypeViewFactory<TView> TypeViewFactory<TView>(this IServiceProvider services)
        where TView : class
        => services.TypeViewFactory().For<TView>();
}
