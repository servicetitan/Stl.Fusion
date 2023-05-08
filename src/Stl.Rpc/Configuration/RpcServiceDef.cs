using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServiceDef
{
    private readonly Dictionary<MethodInfo, RpcMethodDef> _methods;
    private readonly Dictionary<Symbol, RpcMethodDef> _methodByName;

    public RpcHub Hub { get; }
    public Type Type { get; }
    public Type ServerType { get; }
    public Type ClientType { get; }
    public Symbol Name { get; }
    public bool IsSystem { get; }
    public bool HasDefaultServerType => ServerType == Type;
    public bool HasDefaultClientType => ClientType == Type;
    public int MethodCount => _methods.Count;
    public IEnumerable<RpcMethodDef> Methods => _methods.Values;

    public RpcMethodDef this[MethodInfo method] => Get(method) ?? throw Errors.NoMethod(Type, method);
    public RpcMethodDef this[Symbol methodName] => Get(methodName) ?? throw Errors.NoMethod(Type, methodName);

    public RpcServiceDef(RpcHub hub, Symbol name, RpcServiceConfiguration source, Func<RpcMethodDef, Symbol> methodNameBuilder)
    {
        Hub = hub;
        Name = name;
        Type = source.Type;
        ServerType = source.ServerType;
        ClientType = source.ClientType;
        IsSystem = typeof(IRpcSystemService).IsAssignableFrom(Type);

        _methods = new Dictionary<MethodInfo, RpcMethodDef>();
        _methodByName = new Dictionary<Symbol, RpcMethodDef>();
        foreach (var method in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
            if (method.IsGenericMethodDefinition)
                continue;

            var attr = IsSystem ? method.GetCustomAttribute<RpcMethodAttribute>(true) : null;
            var methodDefType = attr?.MethodDefType ?? typeof(RpcMethodDef);
            var methodDef = (RpcMethodDef)methodDefType.CreateInstance(this, method, methodNameBuilder);
            if (!methodDef.IsValid)
                continue;

            if (_methodByName.ContainsKey(methodDef.Name))
                throw Errors.MethodNameConflict(methodDef);

            _methods.Add(method, methodDef);
            _methodByName.Add(methodDef.Name, methodDef);
        }
    }

    public override string ToString()
    {
        var serverTypeInfo = HasDefaultServerType ? "" : $", Serving: {ServerType.GetName()}";
        return $"{GetType().Name}({Type.GetName()}, Name: '{Name}', {MethodCount} method(s){serverTypeInfo})";
    }

    public RpcMethodDef? Get(MethodInfo method) => _methods.GetValueOrDefault(method);
    public RpcMethodDef? Get(Symbol methodName) => _methodByName.GetValueOrDefault(methodName);
}
