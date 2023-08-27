using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcSystemCallSender(IServiceProvider services)
    : RpcServiceBase(services)
{
    private IRpcSystemCalls? _client;
    private RpcServiceDef? _systemCallsServiceDef;
    private RpcMethodDef? _okMethodDef;
    private RpcMethodDef? _errorMethodDef;
    private RpcMethodDef? _cancelMethodDef;
    private RpcMethodDef? _notFoundMethodDef;
    private RpcMethodDef? _streamAckMethodDef;
    private RpcMethodDef? _streamItemMethodDef;
    private RpcMethodDef? _streamEndMethodDef;

    public IRpcSystemCalls Client => _client
        ??= Services.GetRequiredService<IRpcSystemCalls>();
    public RpcServiceDef SystemCallsServiceDef => _systemCallsServiceDef
        ??= Hub.ServiceRegistry.Get<IRpcSystemCalls>()!;
    public RpcMethodDef OkMethodDef => _okMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Ok)));
    public RpcMethodDef ErrorMethodDef => _errorMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Error)));
    public RpcMethodDef CancelMethodDef => _cancelMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Cancel)));
    public RpcMethodDef NotFoundMethodDef => _notFoundMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.NotFound)));
    public RpcMethodDef StreamAckMethodDef => _streamAckMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamAck)));
    public RpcMethodDef StreamItemMethodDef => _streamItemMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamItem)));
    public RpcMethodDef StreamEndMethodDef => _streamEndMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamEnd)));

    public ValueTask Complete<TResult>(RpcPeer peer, long callId,
        Result<TResult> result, bool allowPolymorphism,
        List<RpcHeader>? headers = null)
        => result.IsValue(out var value)
            ? Ok(peer, callId, value, allowPolymorphism, headers)
            : Error(peer, callId, result.Error!, headers);

    public ValueTask Ok<TResult>(RpcPeer peer, long callId,
        TResult result, bool allowPolymorphism,
        List<RpcHeader>? headers = null)
    {
        var headerCount = headers?.Count ?? 0;
        try {
            var context = new RpcOutboundContext(headers) {
                Peer = peer,
                RelatedCallId = callId,
            };
            var call = context.PrepareCall(OkMethodDef, ArgumentList.New(result))!;
            return call.SendNoWait(allowPolymorphism);
        }
        catch (Exception error) {
            if (headers != null) {
                while (headers.Count > headerCount)
                    headers.RemoveAt(headers.Count - 1);
                if (headers.Count == 0)
                    headers = null;
            }
            return Error(peer, callId, error, headers);
        }
    }

    public ValueTask Error(RpcPeer peer, long callId, Exception error, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        var call = context.PrepareCall(ErrorMethodDef, ArgumentList.New(error.ToExceptionInfo()))!;
        return call.SendNoWait(false);
    }

    public ValueTask Cancel(RpcPeer peer, long callId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        var call = context.PrepareCall(CancelMethodDef, ArgumentList.Empty)!;
        return call.SendNoWait(false);
    }

    public ValueTask StreamAck(RpcPeer peer, long streamId, long offset, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = streamId,
        };
        var call = context.PrepareCall(StreamAckMethodDef, ArgumentList.New(offset))!;
        return call.SendNoWait(false);
    }

    public ValueTask StreamItem<TItem>(RpcPeer peer, long streamId, TItem result, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = streamId,
        };
        var call = context.PrepareCall(StreamItemMethodDef, ArgumentList.New(result))!;
        return call.SendNoWait(true);
    }

    public ValueTask StreamEnd(RpcPeer peer, long streamId, Exception? error, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = streamId,
        };
        // An optimized version of Client.Error(result):
        var call = context.PrepareCall(StreamEndMethodDef, ArgumentList.New(error.ToExceptionInfo()))!;
        return call.SendNoWait(false);
    }
}
