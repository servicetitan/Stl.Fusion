using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public interface IRpcComputeSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Invalidate();
}

public class RpcComputeSystemCalls : RpcServiceBase, IRpcComputeSystemCalls
{
    public static readonly Symbol Name = "$c-sys";

    public RpcComputeSystemCalls(IServiceProvider services) : base(services)
    { }

    public Task<RpcNoWait> Invalidate()
    {
        var context = RpcInboundContext.Current;
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        if (peer.Calls.Outbound.TryGetValue(outboundCallId, out var c) && c is IRpcOutboundComputeCall outboundCall)
            outboundCall.TryInvalidate(context);
        return RpcNoWait.Tasks.Completed;
    }
}
