using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public delegate Task<Channel<RpcMessage>> RpcClientChannelProvider(RpcClientPeer peer, CancellationToken cancellationToken);
