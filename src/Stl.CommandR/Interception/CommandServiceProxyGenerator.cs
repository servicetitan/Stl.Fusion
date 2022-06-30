using Castle.DynamicProxy;
using Stl.Concurrency;
using Stl.Interception.Interceptors;

namespace Stl.CommandR.Interception;

public class CommandServiceProxyGenerator : ProxyGeneratorBase
{
    public static CommandServiceProxyGenerator Default { get; } = new();

    private ConcurrentDictionary<Type, Type> Cache { get; } = new();

    public virtual Type GetProxyType(Type type)
        => Cache.GetOrAddChecked(type, static (type1, self) => {
            var tInterfaces = typeof(ICommandService).IsAssignableFrom(type1)
                ? Array.Empty<Type>()
                : new[] { typeof(ICommandService) };
            var options = new ProxyGenerationOptions();
            var proxyType = ProxyBuilder.CreateClassProxyType(type1, tInterfaces, options);
            return proxyType;
        }, this);
}
