using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public readonly record struct RpcHubInternals(RpcHub RpcHub)
{
    public RpcPeerFactory PeerFactory => RpcHub.PeerFactory;
    public RpcPeerResolver PeerResolver => RpcHub.PeerResolver;
    public RpcInboundContextFactory InboundContextFactory => RpcHub.InboundContextFactory;
    public RpcClientChannelProvider ClientChannelProvider => RpcHub.ClientChannelProvider;
    public RpcSystemCallSender SystemCallSender => RpcHub.SystemCallSender;
}
