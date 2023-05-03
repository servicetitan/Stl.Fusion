using Cysharp.Text;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcServiceRegistry
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _serviceByName = new();
    private readonly Dictionary<Type, RpcServiceDef> _serviceByImplementationType = new();

    public int ServiceCount => _services.Count;
    public IEnumerable<RpcServiceDef> Services => _services.Values;

    public RpcServiceDef this[Symbol serviceName]
        => Get(serviceName) ?? throw Errors.NoService(serviceName);
    public RpcServiceDef this[Type serviceType, bool isImplementation = false]
        => Get(serviceType, isImplementation) ?? throw Errors.NoService(serviceType);

    public RpcServiceRegistry(IServiceProvider services)
    {
        var globalOptions = services.GetRequiredService<RpcConfiguration>();
        var implementations = globalOptions.Implementations;
        foreach (var (serviceType, serviceName) in globalOptions.Services) {
            var implementationType = implementations.GetValueOrDefault(serviceType);
            if (_serviceByName.TryGetValue(serviceName, out var serviceDef))
                throw Errors.ServiceNameConflict(serviceType, serviceDef.Type, serviceName);

            serviceDef = new RpcServiceDef(serviceType, implementationType, serviceName, globalOptions.MethodNameBuilder);
            _services.Add(serviceType, serviceDef);
            _serviceByName.Add(serviceName, serviceDef);
            if (implementationType != null)
                _serviceByImplementationType.Add(implementationType, serviceDef);
        }
    }

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.AppendLine($"{GetType().GetName()}({_services.Count} service(s)):");
        foreach (var serviceDef in _services.Values.OrderBy(s => s.Name)) {
            var implementationInfo = serviceDef.ImplementationType != null
                ? $", Implementation: {serviceDef.ImplementationType.GetName()}"
                : "";
            sb.AppendLine($"- '{serviceDef.Name}' -> {serviceDef.Type.GetName()}, {serviceDef.MethodCount} method(s){implementationInfo}");
        }
        return sb.ToString().TrimEnd();
    }

    public RpcServiceDef? Get(Symbol serviceName)
        => _serviceByName.GetValueOrDefault(serviceName);

    public RpcServiceDef? Get(Type serviceType, bool isImplementation = false)
        => isImplementation
            ? _serviceByImplementationType.GetValueOrDefault(serviceType)
            : _services.GetValueOrDefault(serviceType);
}
