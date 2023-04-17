namespace Stl.Rpc;

public class RpcOptions
{
    public Dictionary<Type, Symbol> ServiceTypes { get; } = new();
    public Dictionary<Symbol, Type> ServiceNames { get; } = new();
    public List<Type> MiddlewareTypes { get; } = new();
}
