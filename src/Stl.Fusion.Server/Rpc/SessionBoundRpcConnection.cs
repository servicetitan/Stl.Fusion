using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class SessionBoundRpcConnection : RpcConnection
{
    public Session Session { get; init; }

    public SessionBoundRpcConnection(Channel<RpcMessage> channel, ImmutableOptionSet options, Session session)
        : base(channel, options)
        => Session = session;
}
