namespace Stl.Interception;

public static class ProxyExt
{
    private const string InterceptorsFieldName = "__interceptors";
    private static readonly ConcurrentDictionary<Type, Func<object, object>> InterceptorGettersCache = new();

    public static IEnumerable<object> GetInterceptors(object proxy)
    {
        var getter = InterceptorGettersCache.GetOrAdd(proxy.GetType(),
            static t => {
                var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                return t.GetField(InterceptorsFieldName, bindingFlags)!.GetGetter();
            });
        return (IEnumerable<object>)getter.Invoke(proxy);
    }

    public static IServiceProvider GetServices(object proxy)
    {
        foreach (var interceptor in GetInterceptors(proxy)) {
            if (interceptor is IHasServices hasServices)
                return hasServices.Services;
        }
        throw new InvalidOperationException("Provided proxy doesn't have an interceptor that implements IHasServices.");
    }
}
