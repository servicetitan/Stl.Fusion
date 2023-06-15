using System.Text;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public sealed class RpcServiceRegistry : RpcServiceBase, IReadOnlyCollection<RpcServiceDef>
{
    private readonly Dictionary<Type, RpcServiceDef> _services = new();
    private readonly Dictionary<Symbol, RpcServiceDef> _serviceByName = new();

    private ILogger Log { get; }

    public int Count => _serviceByName.Count;
    public RpcServiceDef this[Type serviceType] => Get(serviceType) ?? throw Errors.NoService(serviceType);
    public RpcServiceDef this[Symbol serviceName] => Get(serviceName) ?? throw Errors.NoService(serviceName);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<RpcServiceDef> GetEnumerator() => _serviceByName.Values.GetEnumerator();

    public RpcServiceRegistry(IServiceProvider services)
        : base(services)
    {
        var hub = Hub; // The implicit RpcHub resolution here freezes RpcConfiguration
        Log = hub.Services.LogFor(GetType());
        foreach (var (_, service) in hub.Configuration.Services) {
            var name = service.Name;
            if (name.IsEmpty)
                name = hub.ServiceNameBuilder.Invoke(service.Type);

            if (_serviceByName.TryGetValue(name, out var serviceDef))
                throw Errors.ServiceNameConflict(service.Type, serviceDef.Type, name);

            serviceDef = new RpcServiceDef(hub, name, service);
            if (_services.ContainsKey(serviceDef.Type))
                throw Errors.ServiceTypeConflict(service.Type);

            _services.Add(serviceDef.Type, serviceDef);
            _serviceByName.Add(serviceDef.Name, serviceDef);
        }
        DumpTo(Log, LogLevel.Debug, "Registered services:");
    }

    public override string ToString()
        => $"{GetType().GetName()}({_services.Count} service(s))";

    public string Dump(bool dumpMethods = true, string indent = "")
    {
        var sb = StringBuilderExt.Acquire();
        DumpTo(sb, dumpMethods, indent);
        return sb.ToStringAndRelease().TrimEnd();
    }

    public void DumpTo(ILogger? log, LogLevel logLevel, string title, bool dumpMethods = true)
    {
        log = log.IfEnabled(logLevel);
        if (log == null)
            return;

        var sb = StringBuilderExt.Acquire();
        sb.AppendLine(title);
        DumpTo(sb, dumpMethods);
        log.Log(logLevel, sb.ToStringAndRelease().TrimEnd());
    }

    public void DumpTo(StringBuilder sb, bool dumpMethods = true, string indent = "")
    {
        foreach (var serviceDef in _serviceByName.Values.OrderBy(s => s.Name)) {
            var serverInfo = serviceDef.HasServer  ? $" -> {serviceDef.ServerResolver}" : "";
            sb.AppendLine($"{indent}'{serviceDef.Name}': {serviceDef.Type.GetName()}{serverInfo}, {serviceDef.MethodCount} method(s)");
            if (!dumpMethods)
                continue;

            foreach (var methodDef in serviceDef.Methods.OrderBy(m => m.Name)) {
                var arguments = methodDef.RemoteParameterTypes.Select(t => t.GetName()).ToDelimitedString();
                sb.AppendLine($"{indent}- '{methodDef.Name}': ({arguments}) -> {methodDef.UnwrappedReturnType.GetName()}");
            }
        }
    }

    public RpcServiceDef? Get<TService>()
        => Get(typeof(TService));

    public RpcServiceDef? Get(Type serviceType)
        => _services.GetValueOrDefault(serviceType);

    public RpcServiceDef? Get(Symbol serviceName)
        => _serviceByName.GetValueOrDefault(serviceName);
}
