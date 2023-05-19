namespace Stl.Rpc.Infrastructure;

public interface IRpcCallValidator
{
    void ValidateCall(RpcInboundContext context, Type[] argumentTypes);
}
