using Cysharp.Text;
using Stl.Interception.Internal;

namespace Stl.Interception;

public static class Proxies
{
    private static readonly ConcurrentDictionary<Type, Type?> Cache = new();

    // New

    public static TType New<TType>(Interceptor interceptor, object? proxyTarget = null)
        where TType : class
    {
        var proxy = (TType)GetProxyType(typeof(TType)).CreateInstance();
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static TType New<TType, T1>(T1 arg1, Interceptor interceptor, object? proxyTarget = null)
        where TType : class
    {
        var proxy = (TType)GetProxyType(typeof(TType)).CreateInstance(arg1);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static TType New<TType, T1, T2>(T1 arg1, T2 arg2, Interceptor interceptor, object? proxyTarget = null)
        where TType : class
    {
        var proxy = (TType)GetProxyType(typeof(TType)).CreateInstance(arg1, arg2);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static TType New<TType, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Interceptor interceptor, object? proxyTarget = null)
        where TType : class
    {
        var proxy = (TType)GetProxyType(typeof(TType)).CreateInstance(arg1, arg2, arg3);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static TType New<TType, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Interceptor interceptor, object? proxyTarget = null)
        where TType : class
    {
        var proxy = (TType)GetProxyType(typeof(TType)).CreateInstance(arg1, arg2, arg3, arg4);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy New(Type type, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxy = (IProxy)GetProxyType(type).CreateInstance();
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy New<T1>(Type type, T1 arg1, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxy = (IProxy)GetProxyType(type).CreateInstance(arg1);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy New<T1, T2>(Type type, T1 arg1, T2 arg2, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxy = (IProxy)GetProxyType(type).CreateInstance(arg1, arg2);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy New<T1, T2, T3>(Type type, T1 arg1, T2 arg2, T3 arg3, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxy = (IProxy)GetProxyType(type).CreateInstance(arg1, arg2, arg3);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    public static IProxy New<T1, T2, T3, T4>(Type type, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Interceptor interceptor, object? proxyTarget = null)
    {
        var proxy = (IProxy)GetProxyType(type).CreateInstance(arg1, arg2, arg3, arg4);
        return interceptor.AttachTo(proxy, proxyTarget);
    }

    // GetProxyType

    public static Type GetProxyType<TType>()
        where TType : class
        => GetProxyType(typeof(TType));

    public static Type GetProxyType(Type type)
        => TryGetProxyType(type) ?? throw Errors.NoProxyType(type);

    public static Type? TryGetProxyType(Type type)
        => Cache.GetOrAdd(type, static type1 => {
            var proxyTypeName = ZString.Concat(
                type1.Namespace,
                ".StlInterceptionProxies.",
                type1.Name,
                "Proxy");
            return type1.Assembly.GetType(proxyTypeName);
        });
}
