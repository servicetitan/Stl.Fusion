using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcComputeSystemCalls : IRpcSystemService
{
    Task<RpcNoWait> Invalidate();
}

public class RpcComputeSystemCalls : RpcServiceBase, IRpcComputeSystemCalls
{
    public static readonly Symbol Name = "$sys-c";

    public RpcComputeSystemCalls(IServiceProvider services) : base(services)
    { }

    public Task<RpcNoWait> Invalidate()
    {
        var context = RpcInboundContext.GetCurrent();
        var peer = context.Peer;
        var outboundCallId = context.Message.CallId;
        if (peer.OutboundCalls.Get(outboundCallId) is IRpcOutboundComputeCall outboundCall)
            outboundCall.TryInvalidate(context);
        return RpcNoWait.Tasks.Completed;
    }
}
