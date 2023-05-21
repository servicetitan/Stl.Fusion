using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcSystemCallSender : RpcServiceBase
{
    private IRpcSystemCalls? _client;
    private RpcServiceDef? _systemCallsServiceDef;
    private RpcMethodDef? _okMethodDef;
    private RpcMethodDef? _errorMethodDef;
    private RpcMethodDef? _cancelMethodDef;

    private IRpcSystemCalls Client => _client
        ??= Services.GetRequiredService<IRpcSystemCalls>();
    private RpcServiceDef SystemCallsServiceDef => _systemCallsServiceDef
        ??= Hub.ServiceRegistry.Get<IRpcSystemCalls>()!;
    private RpcMethodDef OkMethodDef => _okMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Ok)));
    private RpcMethodDef ErrorMethodDef => _errorMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Error)));
    private RpcMethodDef CancelMethodDef => _cancelMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Cancel)));

    public RpcSystemCallSender(IServiceProvider services) : base(services)
    { }

    public ValueTask Complete<TResult>(RpcPeer peer, long callId, Result<TResult> result)
        => result.IsValue(out var value)
            ? Ok(peer, callId, value)
            : Error(peer, callId, result.Error!);

    public async ValueTask Ok<TResult>(RpcPeer peer, long callId, TResult result)
    {
        var context = new RpcOutboundContext {
            Peer = peer,
            RelatedCallId = callId,
            MethodDef = OkMethodDef,
            Arguments = ArgumentList.New(result),
        };
        // An optimized version of Client.Ok(result):
        context.Call = context.MethodDef.CallFactory.CreateOutbound(context);
        var message = context.Call.CreateMessage(callId);
        await peer.Send(message, default).ConfigureAwait(false);
    }

    public async ValueTask Error(RpcPeer peer, long callId, Exception error)
    {
        var context = new RpcOutboundContext {
            Peer = peer,
            RelatedCallId = callId,
            MethodDef = ErrorMethodDef,
            Arguments = ArgumentList.New(error.ToExceptionInfo()),
        };
        // An optimized version of Client.Error(result):
        context.Call = context.MethodDef.CallFactory.CreateOutbound(context);
        var message = context.Call.CreateMessage(callId);
        await peer.Send(message, default).ConfigureAwait(false);
    }

    public async ValueTask Cancel(RpcPeer peer, long callId)
    {
        var context = new RpcOutboundContext {
            Peer = peer,
            RelatedCallId = callId,
            MethodDef = CancelMethodDef,
            Arguments = ArgumentList.Empty,
        };
        // An optimized version of Client.Error(result):
        context.Call = context.MethodDef.CallFactory.CreateOutbound(context);
        var message = context.Call.CreateMessage(callId);
        await peer.Send(message, default).ConfigureAwait(false);
    }
}
