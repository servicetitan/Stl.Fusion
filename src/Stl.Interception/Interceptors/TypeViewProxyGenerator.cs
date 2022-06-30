using Castle.DynamicProxy;
using Stl.Concurrency;

namespace Stl.Interception.Interceptors;

public class TypeViewProxyGenerator : ProxyGeneratorBase
{
    public static TypeViewProxyGenerator Default { get; } = new();

    private ConcurrentDictionary<(Type, Type), Type> Cache { get; } = new();

    public virtual Type GetProxyType(Type targetType, Type viewType)
        => Cache.GetOrAddChecked((targetType, viewType), static (key, self) => {
            var (tTarget, tView) = key;
            var options = new ProxyGenerationOptions {
                BaseTypeForInterfaceProxy = typeof(TypeView<,>).MakeGenericType(tTarget, tView)
            };
            var proxyType = ProxyBuilder.CreateInterfaceProxyTypeWithoutTarget(tView, null, options);
            return proxyType;
        }, this);
}
