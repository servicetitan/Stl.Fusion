using Stl.Internal;

namespace Stl.Rpc;

public class RpcServiceConfiguration
{
    public Type Type { get; }
    public Type ServerType { get; set; }
    public Type ClientType { get; set; }
    public Symbol Name { get; set; }

    public RpcServiceConfiguration(Type type)
    {
        if (type.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(type));

        Type = type;
        ServerType = type;
        ClientType = type;
    }

    public RpcServiceConfiguration Named(Symbol name)
    {
        Name = name;
        return this;
    }

    public RpcServiceConfiguration Serving<TServer>()
        => Serving(typeof(TServer));
    public RpcServiceConfiguration Serving(Type serverType)
    {
        if (!Type.IsAssignableFrom(serverType))
            throw Errors.MustBeAssignableTo(Type, serverType, nameof(serverType));

        ServerType = serverType;
        return this;
    }

    public RpcServiceConfiguration ConsumedAs<TClient>()
        => ConsumedAs(typeof(TClient));
    public RpcServiceConfiguration ConsumedAs(Type clientType)
    {
        if (!Type.IsAssignableFrom(clientType))
            throw Errors.MustBeAssignableTo(Type, clientType, nameof(clientType));

        ClientType = clientType;
        return this;
    }
}
