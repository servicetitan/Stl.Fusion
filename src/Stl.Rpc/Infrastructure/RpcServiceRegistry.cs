using Cysharp.Text;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcServiceRegistry
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _servicesByName = new();
    private readonly Dictionary<Type, Type> _implementations = new();

    public int ServiceCount => _services.Count;
    public IEnumerable<RpcServiceDef> Services => _services.Values;

    public int ImplementationCount => _implementations.Count;
    public IEnumerable<(Type ImplementationType, Type ServiceType)> Implementations
        => _implementations.Select(kv => (kv.Key, kv.Value));

    public RpcServiceDef this[Symbol serviceName] => Get(serviceName) ?? throw Errors.NoService(serviceName);
    public RpcServiceDef this[Type serviceType] => Get(serviceType) ?? throw Errors.NoService(serviceType);

    public RpcServiceRegistry(IServiceProvider services)
    {
        var globalOptions = services.GetRequiredService<RpcGlobalOptions>();
        foreach (var (serviceType, (serviceName, mappedToType)) in globalOptions.ServiceTypes) {
            if (mappedToType == null) {
                if (_servicesByName.TryGetValue(serviceName, out var serviceDef))
                    throw Errors.ServiceNameConflict(serviceType, serviceDef.Type, serviceName);

                serviceDef = new RpcServiceDef(serviceType, serviceName, globalOptions.MethodNameBuilder);
                _services.Add(serviceType, serviceDef);
                _servicesByName.Add(serviceName, serviceDef);
            }
            else
                _implementations.Add(serviceType, mappedToType);
        }
    }

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.AppendLine($"{GetType().GetName()}({_services.Count} service(s)):");
        foreach (var serviceDef in _services.Values.OrderBy(s => s.Name))
            sb.AppendLine($"- '{serviceDef.Name}' -> {serviceDef.Type.GetName()}, {serviceDef.MethodCount} method(s)");
        foreach (var (implementationType, serviceType) in _implementations.OrderBy(kv => kv.Key.GetName(), StringComparer.Ordinal))
            sb.AppendLine($"- Implementation {implementationType.GetName()} -> {serviceType.GetName()}");
        return sb.ToString().TrimEnd();
    }

    public RpcServiceDef? Get(Symbol serviceName) 
        => _servicesByName.GetValueOrDefault(serviceName);

    public RpcServiceDef? Get(Type serviceType)
    {
        serviceType = _implementations.GetValueOrDefault(serviceType) ?? serviceType;
        return _services.GetValueOrDefault(serviceType) ?? throw Errors.NoService(serviceType);
    }
}
