namespace Stl.Rpc.Infrastructure;

public interface IRpcArgumentListTypeResolver
{
    Type? GetArgumentListType(RpcInboundContext context);
}
