using Stl.Rpc.Infrastructure;

namespace Stl.Rpc.Internal;

public readonly record struct RpcPeerInternalServices(RpcPeer Peer)
{
    public ILogger Log => Peer.Log;
    public ILogger? CallLog => Peer.CallLog;
    public IMomentClock Clock => Peer.Clock;
    public ChannelWriter<RpcMessage>? Sender => Peer.Sender;
}
