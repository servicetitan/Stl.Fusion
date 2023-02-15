using Castle.DynamicProxy;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Interception;

public class ComputeServiceProxyGenerator : ProxyGeneratorBase
{
    public static ComputeServiceProxyGenerator Default { get; } = new();

    private ConcurrentDictionary<Type, Type> Cache { get; } = new();

    public virtual Type GetProxyType(Type type)
        => Cache.GetOrAdd(type, static (type1, self) => {
            var tInterfaces = typeof(IComputeService).IsAssignableFrom(type1)
                ? Array.Empty<Type>()
                : new[] { typeof(IComputeService) };
            var options = new ProxyGenerationOptions();
            var proxyType = ProxyBuilder.CreateClassProxyType(type1, tInterfaces, options);
            return proxyType;
        }, this);
}
