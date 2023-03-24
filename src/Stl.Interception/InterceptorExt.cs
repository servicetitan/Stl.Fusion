namespace Stl.Interception;

public static class InterceptorExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T BindTo<T>(this Interceptor interceptor, T proxy, object? proxyTarget = null)
        where T : class, IRequiresAsyncProxy
    {
        interceptor.BindTo(proxy.RequireProxy(), proxyTarget);
        return proxy;
    }
}
