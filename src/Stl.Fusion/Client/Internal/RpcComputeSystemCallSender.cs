using Stl.Interception;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

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
        ??= ComputeSystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcComputeSystemCalls.Invalidate)));

    public RpcComputeSystemCallSender(IServiceProvider services) : base(services)
    { }

    public async ValueTask Invalidate(RpcPeer peer, long callId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        // An optimized version of Client.Error(result):
        var call = context.SetCall(InvalidateMethodDef, ArgumentList.Empty)!;
        await call.Send().ConfigureAwait(false);
    }
}
