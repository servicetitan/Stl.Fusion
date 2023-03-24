using Stl.Interception.Internal;

namespace Stl.Interception;

public class Interceptor
{
    public T AttachTo<T>(T proxy, object? proxyTarget = null)
        where T : class, IRequiresAsyncProxy
    {
        proxy.RequireProxy().SetInterceptor(this);
        if (proxyTarget != null)
            proxy.RequireProxy<InterfaceProxy>().ProxyTarget = proxyTarget;
        if (proxy is INotifyInitialized notifyInitialized)
            notifyInitialized.Initialized();
        return proxy;
    }

    public virtual void Intercept(Invocation invocation)
        => invocation.Intercepted();

    public virtual TResult Intercept<TResult>(Invocation invocation)
        => invocation.Intercepted<TResult>();
}
