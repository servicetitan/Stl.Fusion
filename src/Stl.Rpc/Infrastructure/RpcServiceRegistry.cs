namespace Stl.Rpc.Infrastructure;

public class RpcServiceRegistry
{
    private readonly Dictionary<Symbol, Type> _typeByName;
    private readonly Dictionary<Type, Symbol> _nameByType;

    public Type? this[Symbol symbol] => _typeByName.GetValueOrDefault(symbol);
    public Symbol this[Type serviceType] => _nameByType.GetValueOrDefault(serviceType);

    public RpcServiceRegistry(IServiceProvider services)
    {
        _typeByName = new Dictionary<Symbol, Type>(services.GetRequiredService<RpcOptions>().ServiceNames);
        _nameByType = new Dictionary<Type, Symbol>(services.GetRequiredService<RpcOptions>().ServiceTypes);
    }
}
