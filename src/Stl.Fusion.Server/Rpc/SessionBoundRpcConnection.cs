using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class SessionBoundRpcConnection(Channel<RpcMessage> channel, ImmutableOptionSet options, Session session)
    : RpcConnection(channel, options)
{
    public Session Session { get; init; } = session;
}
