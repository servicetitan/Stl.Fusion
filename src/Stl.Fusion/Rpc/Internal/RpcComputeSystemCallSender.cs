using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public sealed class RpcComputeSystemCallSender : RpcServiceBase
{
    private IRpcComputeSystemCalls? _client;
    private RpcServiceDef? _computeSystemCallsServiceDef;
    private RpcMethodDef? _invalidateMethodDef;

    private IRpcComputeSystemCalls Client => _client
        ??= Services.GetRequiredService<IRpcComputeSystemCalls>();
    private RpcServiceDef ComputeSystemCallsServiceDef => _computeSystemCallsServiceDef
        ??= Hub.ServiceRegistry.Get<IRpcComputeSystemCalls>()!;
    private RpcMethodDef InvalidateMethodDef => _invalidateMethodDef
        ??= ComputeSystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Error)));

    public RpcComputeSystemCallSender(IServiceProvider services) : base(services)
    { }

    public ValueTask Invalidate(RpcPeer peer, long callId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        // An optimized version of Client.Error(result):
        var call = context.Bind(InvalidateMethodDef, ArgumentList.Empty)!;
        var message = call.CreateMessage(callId);
        return peer.Send(message, default);
    }
}
