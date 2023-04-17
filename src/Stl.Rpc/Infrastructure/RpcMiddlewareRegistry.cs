namespace Stl.Rpc.Infrastructure;

public class RpcMiddlewareRegistry
{
    public IReadOnlyList<Type> MiddlewareTypes { get; }

    public RpcMiddlewareRegistry(IServiceProvider services)
    {
        var middlewareTypes = services.GetRequiredService<RpcOptions>().MiddlewareTypes.ToArray();
        Array.Reverse(middlewareTypes);
        MiddlewareTypes = middlewareTypes;
    }
}
