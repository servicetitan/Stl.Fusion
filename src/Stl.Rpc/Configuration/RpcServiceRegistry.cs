using Cysharp.Text;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServiceRegistry : RpcServiceBase, IReadOnlyCollection<RpcServiceDef>
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _serviceByName = new();

    public int Count => _serviceByName.Count;
    public RpcServiceDef this[Type serviceType] => Get(serviceType) ?? throw Errors.NoService(serviceType);
    public RpcServiceDef this[Symbol serviceName] => Get(serviceName) ?? throw Errors.NoService(serviceName);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<RpcServiceDef> GetEnumerator() => _serviceByName.Values.GetEnumerator();

    public RpcServiceRegistry(IServiceProvider services)
        : base(services)
    {
        var hub = Hub;
        var configuration = hub.Configuration;
        foreach (var (_, service) in configuration.Services) {
            var name = service.Name;
            if (name.IsEmpty)
                name = configuration.ServiceNameBuilder.Invoke(service.Type);

            if (_serviceByName.TryGetValue(name, out var serviceDef))
                throw Errors.ServiceNameConflict(service.Type, serviceDef.Type, name);

            serviceDef = new RpcServiceDef(hub, name, service, configuration.MethodNameBuilder);
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

    public RpcServiceDef? Get<TService>()
        => Get(typeof(TService));

    public RpcServiceDef? Get(Type serviceType)
        => _services.GetValueOrDefault(serviceType);

    public RpcServiceDef? Get(Symbol serviceName)
        => _serviceByName.GetValueOrDefault(serviceName);
}
