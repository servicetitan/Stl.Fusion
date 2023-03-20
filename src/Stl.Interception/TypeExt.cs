using Cysharp.Text;
using Stl.Interception.Internal;

namespace Stl.Interception;

public static class TypeExt
{
    private static readonly ConcurrentDictionary<Type, Type?> Cache = new();

    public static Type GetProxyType(this Type type)
        => type.TryGetProxyType() ?? throw Errors.NoProxyType(type);

    public static Type? TryGetProxyType(this Type type)
        => Cache.GetOrAdd(type, static type1 => {
            var proxyTypeName = ZString.Concat(
                type1.Namespace,
                ".StlInterceptionProxies.",
                type1.Name,
                "Proxy");
            return type1.Assembly.GetType(proxyTypeName);
        });
}
