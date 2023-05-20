namespace Stl.Rpc.Infrastructure;

public interface IRpcArgumentTypeResolver
{
    void ResolveArgumentTypes(RpcInboundContext context, Type[] argumentTypes);
}
