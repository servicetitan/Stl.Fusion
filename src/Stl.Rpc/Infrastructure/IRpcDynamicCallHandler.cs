using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public interface IRpcDynamicCallHandler
{
    bool IsValidCall(RpcInboundContext context, ref ArgumentList arguments, ref bool allowPolymorphism);
}
