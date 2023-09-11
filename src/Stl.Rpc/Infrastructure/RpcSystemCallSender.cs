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
    private RpcMethodDef? _keepAliveMethodDef;
    private RpcMethodDef? _missingObjectsMethodDef;
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
    public RpcMethodDef KeepAliveMethodDef => _keepAliveMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.KeepAlive)));
    public RpcMethodDef MissingObjectsMethodDef => _missingObjectsMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.MissingObjects)));
    public RpcMethodDef StreamAckMethodDef => _streamAckMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamAck)));
    public RpcMethodDef StreamItemMethodDef => _streamItemMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamItem)));
    public RpcMethodDef StreamEndMethodDef => _streamEndMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.StreamEnd)));

    public Task Complete<TResult>(RpcPeer peer, long callId,
        Result<TResult> result, bool allowPolymorphism,
        List<RpcHeader>? headers = null)
        => result.IsValue(out var value)
            ? Ok(peer, callId, value, allowPolymorphism, headers)
            : Error(peer, callId, result.Error!, headers);

    public Task Ok<TResult>(RpcPeer peer, long callId,
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

    public Task Error(RpcPeer peer, long callId, Exception error, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        var call = context.PrepareCall(ErrorMethodDef, ArgumentList.New(error.ToExceptionInfo()))!;
        return call.SendNoWait(false);
    }

    public Task Cancel(RpcPeer peer, long callId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = callId,
        };
        var call = context.PrepareCall(CancelMethodDef, ArgumentList.Empty)!;
        return call.SendNoWait(false);
    }

    public Task KeepAlive(RpcPeer peer, long[] objectIds, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
        };
        var call = context.PrepareCall(KeepAliveMethodDef, ArgumentList.New(objectIds))!;
        return call.SendNoWait(false);
    }

    public Task MissingObjects(RpcPeer peer, long[] objectIds, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
        };
        var call = context.PrepareCall(MissingObjectsMethodDef, ArgumentList.New(objectIds))!;
        return call.SendNoWait(false);
    }

    public Task StreamAck(RpcPeer peer, long objectId, long nextIndex, bool mustReset, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        var call = context.PrepareCall(StreamAckMethodDef, ArgumentList.New(nextIndex, mustReset))!;
        return call.SendNoWait(false);
    }

    public Task StreamItem<TItem>(RpcPeer peer, long objectId, long index, long ackIndex, TItem result, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        var call = context.PrepareCall(StreamItemMethodDef, ArgumentList.New(index, ackIndex, result))!;
        return call.SendNoWait(true);
    }

    public Task StreamEnd(RpcPeer peer, long objectId, long index, Exception? error, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        // An optimized version of Client.Error(result):
        var call = context.PrepareCall(StreamEndMethodDef, ArgumentList.New(index, error.ToExceptionInfo()))!;
        return call.SendNoWait(false);
    }
}
