using Stl.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public record RpcChannelOptions
{
    public Func<Type, bool> MiddlewareFilter { get; init; } = static _ => true;
    public Func<RpcServiceDef, bool> ServiceFilter { get; init; } = static _ => true;

    public Func<ArgumentList, Type, object?>? Serializer { get; init; }
    public Func<object?, Type, ArgumentList>? Deserializer { get; init; }

    public static Func<RpcChannel, RpcChannelOptions> DefaultOptionsProvider(IServiceProvider services)
        => _ => new RpcChannelOptions();
}
