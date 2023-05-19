using Stl.Internal;

namespace Stl.Rpc;

public class RpcServiceConfiguration
{
    public Type Type { get; }
    public Type ServerType { get; set; }
    public Type ClientType { get; set; }
    public Symbol Name { get; set; }

    public RpcServiceConfiguration(Type type, Symbol name = default)
    {
        if (type.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(type));

        Type = type;
        ServerType = type;
        ClientType = type;
        Name = name;
    }

    public RpcServiceConfiguration WithName(Symbol name)
    {
        Name = name;
        return this;
    }

    public RpcServiceConfiguration WithServer<TServer>()
        => WithServer(typeof(TServer));
    public RpcServiceConfiguration WithServer(Type serverType)
    {
        if (!Type.IsAssignableFrom(serverType))
            throw Errors.MustBeAssignableTo(Type, serverType, nameof(serverType));

        ServerType = serverType;
        return this;
    }

    public RpcServiceConfiguration WithClient<TClient>()
        => WithClient(typeof(TClient));
    public RpcServiceConfiguration WithClient(Type clientType)
    {
        if (!Type.IsAssignableFrom(clientType))
            throw Errors.MustBeAssignableTo(Type, clientType, nameof(clientType));

        ClientType = clientType;
        return this;
    }
}
