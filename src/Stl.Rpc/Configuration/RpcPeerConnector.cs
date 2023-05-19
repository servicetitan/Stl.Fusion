using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public delegate Task<Channel<RpcMessage>> RpcPeerConnector(RpcPeer peer, CancellationToken cancellationToken);
