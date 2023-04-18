using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

public class RpcServiceDef
{
    private readonly Dictionary<MethodInfo, RpcMethodDef> _methods;
    private readonly Dictionary<Symbol, RpcMethodDef> _methodByName;

    public Type Type { get; }
    public Symbol Name { get; }
    public bool IsSystem { get; }
    public int MethodCount => _methods.Count;
    public IEnumerable<RpcMethodDef> Methods => _methods.Values;

    public RpcMethodDef this[MethodInfo method] => Get(method) ?? throw Errors.NoMethod(Type, method);
    public RpcMethodDef this[Symbol methodName] => Get(methodName) ?? throw Errors.NoMethod(Type, methodName);

    public RpcServiceDef(Type type, Symbol name, Func<RpcMethodDef, Symbol> methodNameBuilder)
    {
        Type = type;
        Name = name;
        IsSystem = typeof(IRpcSystemService).IsAssignableFrom(type);

        _methods = new Dictionary<MethodInfo, RpcMethodDef>();
        _methodByName = new Dictionary<Symbol, RpcMethodDef>();
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
            if (method.IsGenericMethodDefinition)
                continue;

            var methodDef = new RpcMethodDef(this, method, methodNameBuilder);
            if (_methodByName.ContainsKey(methodDef.Name))
                throw Errors.MethodNameConflict(methodDef);

            _methods.Add(method, methodDef);
            _methodByName.Add(methodDef.Name, methodDef);
        }
    }

    public override string ToString()
        => $"{GetType().Name}({Type.GetName()}, Name: '{Name}', {MethodCount} method(s))";

    public RpcMethodDef? Get(MethodInfo method) => _methods.GetValueOrDefault(method);
    public RpcMethodDef? Get(Symbol methodName) => _methodByName.GetValueOrDefault(methodName);
}
