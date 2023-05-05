using Cysharp.Text;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcServiceRegistry
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _serviceByName = new();

    public int ServiceCount => _services.Count;
    public IEnumerable<RpcServiceDef> Services => _services.Values;

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
            _services.Add(serviceDef.Type, serviceDef);
            _serviceByName.Add(serviceDef.Name, serviceDef);
        }
    }

    public override string ToString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.AppendLine($"{GetType().GetName()}({_services.Count} service(s)):");
        foreach (var serviceDef in _services.Values.OrderBy(s => s.Name)) {
            var serverTypeInfo = serviceDef.ServerType != serviceDef.Type
                ? $", Serving: {serviceDef.ServerType.GetName()}"
                : "";
            sb.AppendLine($"- '{serviceDef.Name}' -> {serviceDef.Type.GetName()}, {serviceDef.MethodCount} method(s){serverTypeInfo}");
        }
        return sb.ToString().TrimEnd();
    }

    public RpcServiceDef? Get(Type serviceType)
        => _services.GetValueOrDefault(serviceType);

    public RpcServiceDef? Get(Symbol serviceName)
        => _serviceByName.GetValueOrDefault(serviceName);
}
