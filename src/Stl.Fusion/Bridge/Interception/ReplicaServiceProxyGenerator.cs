using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Interception.Interceptors;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaServiceProxyGenerator : ProxyGeneratorBase
{
    public static ReplicaServiceProxyGenerator Default { get; } = new();

    private ConcurrentDictionary<Type, Type> Cache { get; } = new();

    public virtual Type GetProxyType(Type type)
        => Cache.GetOrAddChecked(type, static (type1, self) => {
            var tInterfaces = typeof(IReplicaService).IsAssignableFrom(type1)
                ? Array.Empty<Type>()
                : new[] { typeof(IReplicaService) };
            var options = new ProxyGenerationOptions();
            var proxyType = ProxyBuilder
                .CreateInterfaceProxyTypeWithTargetInterface(type1, tInterfaces, options);
            return proxyType;
        }, this);
}
