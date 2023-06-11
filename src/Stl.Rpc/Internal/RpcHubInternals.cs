using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public readonly record struct RpcHubInternals(RpcHub RpcHub)
{
    public RpcPeerFactory PeerFactory => RpcHub.PeerFactory;
    public IEnumerable<RpcPeerTracker> PeerTrackers => RpcHub.PeerTrackers;
    public RpcCallRouter CallRouter => RpcHub.CallRouter;
    public RpcInboundContextFactory InboundContextFactory => RpcHub.InboundContextFactory;
    public RpcClientChannelProvider ClientChannelProvider => RpcHub.ClientChannelProvider;
    public RpcClientIdGenerator ClientIdGenerator => RpcHub.ClientIdGenerator;
    public RpcSystemCallSender SystemCallSender => RpcHub.SystemCallSender;
    public RpcErrorClassifier ErrorClassifier => RpcHub.ErrorClassifier;
}
