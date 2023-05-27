using Stl.Internal;

namespace Stl.Rpc;

public record RpcServiceBuilder(
    RpcBuilder Rpc,
    Type Type,
    Type ServerType,
    Symbol Name = default)
{
    public RpcServiceBuilder(RpcBuilder rpc, Type type, Symbol name = default)
        : this(rpc, type, type, name)
    { }

    public RpcServiceBuilder RequireValid()
    {
        if (Type.IsValueType)
            throw Errors.MustBeClass(Type);
        if (!Type.IsAssignableFrom(ServerType))
            throw Errors.MustBeAssignableTo(ServerType, Type, nameof(ServerType));

        return this;
    }

    public RpcServiceBuilder AddClient<TClient>()
        => AddClient(typeof(TClient));
    public RpcServiceBuilder AddClient(Type? clientType = null)
    {
        if (clientType == null)
            clientType = Type;
        else if (!Type.IsAssignableFrom(clientType))
            throw Errors.MustBeAssignableTo(clientType, Type, nameof(clientType));

        Rpc.Services.AddSingleton(clientType, c => c.RpcHub().CreateClient(Type, clientType));
        return this;
    }
}

public record RpcServiceBuilder<TService>(
    RpcBuilder Rpc,
    Type ServerType,
    Symbol Name = default
    ) : RpcServiceBuilder(Rpc, typeof(TService), ServerType, Name)
{
    public RpcServiceBuilder(RpcBuilder rpc, Symbol name = default)
        : this(rpc, typeof(TService), name)
    { }

    public new RpcServiceBuilder<TService> AddClient<TClient>()
        where TClient : TService
        => (RpcServiceBuilder<TService>)base.AddClient(typeof(TClient));
}
