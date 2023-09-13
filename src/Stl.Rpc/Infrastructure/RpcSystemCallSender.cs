using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public sealed class RpcSystemCallSender(IServiceProvider services)
    : RpcServiceBase(services)
{
    private IRpcSystemCalls? _client;
    private RpcServiceDef? _systemCallsServiceDef;
    private RpcMethodDef? _handshakeMethodDef;
    private RpcMethodDef? _okMethodDef;
    private RpcMethodDef? _errorMethodDef;
    private RpcMethodDef? _cancelMethodDef;
    private RpcMethodDef? _notFoundMethodDef;
    private RpcMethodDef? _keepAliveMethodDef;
    private RpcMethodDef? _disconnectMethodDef;
    private RpcMethodDef? _ackMethodDef;
    private RpcMethodDef? _itemMethodDef;
    private RpcMethodDef? _endMethodDef;

    public IRpcSystemCalls Client => _client
        ??= Services.GetRequiredService<IRpcSystemCalls>();
    public RpcServiceDef SystemCallsServiceDef => _systemCallsServiceDef
        ??= Hub.ServiceRegistry.Get<IRpcSystemCalls>()!;
    public RpcMethodDef HandshakeMethodDef => _handshakeMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Handshake)));
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
    public RpcMethodDef DisconnectMethodDef => _disconnectMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Disconnect)));
    public RpcMethodDef AckMethodDef => _ackMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.Ack)));
    public RpcMethodDef ItemMethodDef => _itemMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.I)));
    public RpcMethodDef EndMethodDef => _endMethodDef
        ??= SystemCallsServiceDef.Methods.Single(m => Equals(m.Method.Name, nameof(IRpcSystemCalls.End)));

    // Handshake

    public Task Handshake(RpcPeer peer, RpcHandshake handshake, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
        };
        var call = context.PrepareCall(HandshakeMethodDef, ArgumentList.New(handshake))!;
        return call.SendNoWait(false);
    }

    // Regular calls

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

    // Objects

    public Task KeepAlive(RpcPeer peer, long[] localIds, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
        };
        var call = context.PrepareCall(KeepAliveMethodDef, ArgumentList.New(localIds))!;
        return call.SendNoWait(false);
    }

    public Task Disconnect(RpcPeer peer, long[] localIds, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
        };
        var call = context.PrepareCall(DisconnectMethodDef, ArgumentList.New(localIds))!;
        return call.SendNoWait(false);
    }

    // Streams

    public Task Ack(RpcPeer peer, long objectId, long nextIndex, Guid hostId, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        var call = context.PrepareCall(AckMethodDef, ArgumentList.New(nextIndex, hostId))!;
        return call.SendNoWait(false);
    }

    public Task Item<TItem>(RpcPeer peer, long objectId, long index, TItem result, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        var call = context.PrepareCall(ItemMethodDef, ArgumentList.New(index, result))!;
        return call.SendNoWait(true);
    }

    public Task End(RpcPeer peer, long objectId, long index, Exception? error, List<RpcHeader>? headers = null)
    {
        var context = new RpcOutboundContext(headers) {
            Peer = peer,
            RelatedCallId = objectId,
        };
        // An optimized version of Client.Error(result):
        var call = context.PrepareCall(EndMethodDef, ArgumentList.New(index, error.ToExceptionInfo()))!;
        return call.SendNoWait(false);
    }
}
