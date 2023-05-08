using Stl.Interception.Interceptors;

namespace Stl.Rpc.Infrastructure;

public class RpcHandlerFactory : RpcServiceBase
{
    private static readonly ConcurrentDictionary<(Type GenericType, Type UnwrappedReturnType), Type> HandlerTypeCache = new();

    public RpcHandlerFactory(IServiceProvider services) : base(services) { }

    public virtual RpcHandler Create(MethodDef methodDef)
    {
        if (!methodDef.IsValid)
            throw new ArgumentOutOfRangeException(nameof(methodDef));

        var handlerType = GetHandlerType(typeof(RpcHandler<>), methodDef);
        return (RpcHandler)Services.Activate(handlerType, methodDef);
    }

    // Protected methods

    protected static Type GetHandlerType(Type methodHandlerGenericType, MethodDef methodDef)
        => HandlerTypeCache.GetOrAdd(
            (methodHandlerGenericType, methodDef.UnwrappedReturnType),
            t => t.GenericType.MakeGenericType(t.UnwrappedReturnType));
}
