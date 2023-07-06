using Microsoft.AspNetCore.Http;
using Stl.Fusion.Server.Authentication;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class FusionRpcConnection : RpcConnection
{
    public Session? Session { get; init; }

    public static RpcServerConnectionFactory ServerConnectionFactory { get; set; } = DefaultServerConnectionFactory;

    public FusionRpcConnection(Channel<RpcMessage> channel, ImmutableOptionSet options, Session? session)
        : base(channel, options)
        => Session = session;

    public static Task<RpcConnection> DefaultServerConnectionFactory(
        RpcServerPeer peer, Channel<RpcMessage> channel, ImmutableOptionSet options,
        CancellationToken cancellationToken)
    {
        if (!options.TryGet<HttpContext>(out var httpContext)
            || httpContext.RequestServices.GetService<SessionMiddleware>() is not { } sessionMiddleware)
            return Task.FromResult(new RpcConnection(channel, options));

        var session = sessionMiddleware.GetSession(httpContext);
        return Task.FromResult((RpcConnection)new FusionRpcConnection(channel, options, session!));
    }
}
