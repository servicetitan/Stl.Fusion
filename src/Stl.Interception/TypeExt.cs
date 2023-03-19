using Cysharp.Text;

namespace Stl.Interception;

public static class TypeExt
{
    public static Type? GetProxyType(this Type type)
    {
        var proxyTypeName = ZString.Concat(
            type.Namespace,
            ".StlInterceptionProxies.",
            type.Name,
            "Proxy");
        return type.Assembly.GetType(proxyTypeName);
    }
}
