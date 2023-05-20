using Stl.Internal;

namespace Stl.Rpc;

public record RpcServiceConfiguration(
    Type Type,
    Type ServerType,
    Type ClientType,
    Symbol Name = default)
{
    public RpcServiceConfiguration(Type type, Symbol name = default)
        : this(type, type, type, name)
    { }

    public RpcServiceConfiguration RequireValid(Type type)
    {
        if (type.IsValueType)
            throw Errors.MustBeClass(type, nameof(type));
        if (Type != type)
            throw Internal.Errors.ServiceTypeCannotBeChanged(type, Type);
        if (!Type.IsAssignableFrom(ServerType))
            throw Errors.MustBeAssignableTo(Type, ServerType, nameof(ServerType));
        if (!Type.IsAssignableFrom(ClientType))
            throw Errors.MustBeAssignableTo(Type, ClientType, nameof(ClientType));

        return this;
    }

    public RpcServiceConfiguration With<TServer, TClient>(Symbol? name = default)
        => this with {
            ServerType = typeof(TServer),
            ClientType = typeof(TClient),
            Name = name ?? Name
        };

    public RpcServiceConfiguration With(Type serverType, Type clientType, Symbol? name = default)
        => this with {
            ServerType = serverType,
            ClientType = clientType,
            Name = name ?? Name
        };

    public RpcServiceConfiguration WithServer<TServer>(Symbol? name = default)
        => WithServer(typeof(TServer), name);
    public RpcServiceConfiguration WithServer(Type serverType, Symbol? name = default)
        => this with {
            ServerType = serverType,
            Name = name ?? Name,
        };

    public RpcServiceConfiguration WithClient<TClient>(Symbol? name = default)
        => WithClient(typeof(TClient), name);
    public RpcServiceConfiguration WithClient(Type clientType, Symbol? name = default)
        => this with {
            ClientType = clientType,
            Name = name ?? Name,
        };
}

public record RpcServiceConfiguration<TService>(
    Type ServerType,
    Type ClientType,
    Symbol Name = default
    ) : RpcServiceConfiguration(typeof(TService), ServerType, ClientType, Name)
{
    public RpcServiceConfiguration(RpcServiceConfiguration source)
        : this(source.ServerType, source.ClientType, source.Name)
    {
        if (source.Type != typeof(TService))
            throw Internal.Errors.ServiceTypeCannotBeChanged(source.Type, Type);
    }

    public RpcServiceConfiguration(Symbol name = default)
        : this(typeof(TService), typeof(TService), name)
    { }

    public new RpcServiceConfiguration<TService> With<TServer, TClient>(Symbol? name = default)
        where TServer : TService
        where TClient : TService
        => this with {
            ServerType = typeof(TServer),
            ClientType = typeof(TClient),
            Name = name ?? Name
        };

    public new RpcServiceConfiguration<TService> With(Type serverType, Type clientType, Symbol? name = default)
        => this with {
            ServerType = serverType,
            ClientType = clientType,
            Name = name ?? Name
        };

    public new RpcServiceConfiguration<TService> WithServer<TServer>(Symbol? name = default)
        where TServer : TService
        => WithServer(typeof(TServer), name);
    public new RpcServiceConfiguration<TService> WithServer(Type serverType, Symbol? name = default)
        => this with {
            ServerType = serverType,
            Name = name ?? Name,
        };

    public new RpcServiceConfiguration<TService> WithClient<TClient>(Symbol? name = default)
        where TClient : TService
        => WithClient(typeof(TClient), name);
    public new RpcServiceConfiguration<TService> WithClient(Type clientType, Symbol? name = default)
        => this with {
            ClientType = clientType,
            Name = name ?? Name,
        };
}
