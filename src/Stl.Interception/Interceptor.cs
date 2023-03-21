using Stl.Interception.Internal;

namespace Stl.Interception;

public class Interceptor
{
    public T AttachTo<T>(T proxy, object? proxyTarget = null)
    {
        Cast<T, IProxy>(proxy).Bind(this);
        if (proxyTarget != null)
            Cast<T, InterfaceProxy>(proxy).ProxyTargetUntyped = proxyTarget;
        return proxy;
    }

    public virtual TResult Intercept<TResult>(Invocation invocation)
        => invocation.Intercepted<TResult>();

    public virtual void Intercept(Invocation invocation)
        => invocation.Intercepted();

    // Private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TExpected Cast<T, TExpected>(T proxy)
        => proxy is TExpected expected
            ? expected
            : throw Errors.InvalidProxyType(proxy?.GetType(), typeof(InterfaceProxy));
}
