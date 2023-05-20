namespace Stl.Rpc.Infrastructure;

public sealed class RpcSystemCallSender : RpcServiceBase
{
    public IRpcSystemCallsClient Client { get; }

    public RpcSystemCallSender(IServiceProvider services) : base(services)
        => Client = services.GetRequiredService<IRpcSystemCallsClient>();

    public Task Complete<TResult>(RpcPeer peer, long callId, Result<TResult> result)
    {
        var outboundContext = new RpcOutboundContext();
        using var _ = outboundContext.Activate();
        outboundContext.Peer = peer;
        outboundContext.RelatedCallId = callId;
        return result.IsValue(out var value)
            ? Client.Ok(value)
            : Client.Fail(result.Error.ToExceptionInfo());
    }

    public Task Cancel(RpcPeer peer, long callId)
    {
        var outboundContext = new RpcOutboundContext();
        using var _ = outboundContext.Activate();
        outboundContext.Peer = peer;
        outboundContext.RelatedCallId = callId;
        return Client.Cancel();
    }
}
