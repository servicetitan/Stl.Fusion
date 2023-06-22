using Stl.Internal;

namespace Stl.Rpc;

public sealed class RpcServiceBuilder
{
    public RpcBuilder Rpc { get; }
    public Type Type { get; }
    public Symbol Name { get; set; }
    public ServiceResolver? ServerResolver { get; private set; }

    public RpcServiceBuilder(RpcBuilder rpc, Type type, Symbol name = default)
    {
        if (type.IsValueType)
            throw Errors.MustBeClass(type, nameof(type));

        Rpc = rpc;
        Type = type;
        Name = name;
    }

    public RpcServiceBuilder HasName(Symbol name)
    {
        Name = name;
        return  this;
    }

    public RpcServiceBuilder HasServer<TServer>()
        => HasServer(typeof(TServer));
    public RpcServiceBuilder HasServer(ServiceResolver? serverResolver = null)
    {
        serverResolver ??= ServiceResolver.New(Type);
        if (!Type.IsAssignableFrom(serverResolver.Type))
            throw Errors.MustBeAssignableTo(serverResolver.Type, Type, nameof(serverResolver));

        ServerResolver = serverResolver;
        return this;
    }

    public RpcServiceBuilder HasNoServer()
    {
        ServerResolver = null;
        return this;
    }

    public RpcBuilder Remove()
    {
        Rpc.Configuration.Services.Remove(Type);
        return Rpc;
    }
}
