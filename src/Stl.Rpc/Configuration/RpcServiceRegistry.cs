using Cysharp.Text;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServiceRegistry
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _serviceByName = new();

    public int ServiceCount => _serviceByName.Count;
    public IEnumerable<RpcServiceDef> Services => _serviceByName.Values;

    public RpcServiceDef this[Type serviceType] => Get(serviceType) ?? throw Errors.NoService(serviceType);
    public RpcServiceDef this[Symbol serviceName] => Get(serviceName) ?? throw Errors.NoService(serviceName);

    public RpcServiceRegistry(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<RpcConfiguration>();
        foreach (var (_, service) in configuration.Services) {
            var name = service.Name;
            if (name.IsEmpty)
                name = configuration.ServiceNameBuilder.Invoke(service.Type);

            if (_serviceByName.TryGetValue(name, out var serviceDef))
                throw Errors.ServiceNameConflict(service.Type, serviceDef.Type, name);

            serviceDef = new RpcServiceDef(name, service, configuration.MethodNameBuilder);
            if (_services.ContainsKey(serviceDef.Type))
                throw Errors.ServiceTypeConflict(service.Type);
            if (!serviceDef.HasDefaultServerType && _services.ContainsKey(serviceDef.ServerType))
                throw Errors.ServiceTypeConflict(service.ServerType);
            if (!serviceDef.HasDefaultClientType && _services.ContainsKey(serviceDef.ClientType))
                throw Errors.ServiceTypeConflict(service.ClientType);

            _services.Add(serviceDef.Type, serviceDef);
            if (!serviceDef.HasDefaultServerType)
                _services.Add(serviceDef.ServerType, serviceDef);
            if (!serviceDef.HasDefaultClientType)
                _services.Add(serviceDef.ClientType, serviceDef);
            _serviceByName.Add(serviceDef.Name, serviceDef);
        }
    }

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.AppendLine($"{GetType().GetName()}({_services.Count} service(s)):");
        foreach (var serviceDef in _serviceByName.Values.OrderBy(s => s.Name)) {
            var serverTypeInfo = serviceDef.HasDefaultServerType ? "" : $", Serving: {serviceDef.ServerType.GetName()}";
            sb.AppendLine($"- '{serviceDef.Name}' -> {serviceDef.Type.GetName()}, {serviceDef.MethodCount} method(s){serverTypeInfo}");
        }
        return sb.ToString().TrimEnd();
    }

    public RpcServiceDef? Get(Type serviceType)
        => _services.GetValueOrDefault(serviceType);

    public RpcServiceDef? Get(Symbol serviceName)
        => _serviceByName.GetValueOrDefault(serviceName);
}
