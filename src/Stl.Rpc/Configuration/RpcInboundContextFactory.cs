using Stl.Rpc.Infrastructure;

namespace Stl.Rpc;

public delegate RpcInboundContext RpcInboundContextFactory(RpcPeer peer, RpcMessage message, CancellationToken cancellationToken);
