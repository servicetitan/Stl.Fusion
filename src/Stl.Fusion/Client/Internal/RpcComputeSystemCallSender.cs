using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public sealed class RpcComputeSystemCallSender(IServiceProvider services)
    : RpcServiceBase(services)
{
    private IRpcComputeSystemCalls? _client;
    private RpcServiceDef? _computeSystemCallsServiceDef;
    private RpcMethodDef? _invalidateMethodDef;

    private IRpcComputeSystemCalls Client => _client
        ??= Services.GetRequiredService<IRpcComputeSystemCalls>();
    private RpcServiceDef ComputeSystemCallsServiceDef => _computeSystemCallsServiceDef
        ??= Hub.ServiceRegistry.Get<IRpcComputeSystemCalls>()!;
    private RpcMethodDef InvalidateMethodDef => _invalidateMethodDef
        ??= ComputeSystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcComputeSystemCalls.Invalidate)));

    public Task Invalidate(RpcPeer peer, long callId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        // An optimized version of Client.Error(result):
        var call = context.PrepareCall(InvalidateMethodDef, ArgumentList.Empty)!;
        return call.SendNoWait(false);
    }
}
