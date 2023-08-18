using Microsoft.AspNetCore.Http;
using Stl.Fusion.Server.Middlewares;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class SessionBoundRpcConnectionFactory
{
    public string SessionParameterName { get; init; } = "session";

    public Task<RpcConnection> Invoke(
        RpcServerPeer peer, Channel<RpcMessage> channel, ImmutableOptionSet options,
        CancellationToken cancellationToken)
    {
        if (!options.TryGet<HttpContext>(out var httpContext))
            return RpcConnectionTask(channel, options);

        var query = httpContext.Request.Query;
        var sessionId = query[SessionParameterName].SingleOrDefault() ?? "";
        if (!sessionId.IsNullOrEmpty() && new Session(sessionId) is { } session1 && session1.IsValid())
            return SessionBoundRpcConnectionTask(channel, options, session1);

        var sessionMiddleware = httpContext.RequestServices.GetService<SessionMiddleware>();
        if (sessionMiddleware?.GetSession(httpContext) is { } session2 && session2.IsValid())
            return SessionBoundRpcConnectionTask(channel, options, session2);

        return RpcConnectionTask(channel, options);
    }

    protected static Task<RpcConnection> SessionBoundRpcConnectionTask(
        Channel<RpcMessage> channel, ImmutableOptionSet options, Session session)
        => Task.FromResult<RpcConnection>(new SessionBoundRpcConnection(channel, options, session));

    protected static Task<RpcConnection> RpcConnectionTask(
        Channel<RpcMessage> channel, ImmutableOptionSet options)
        => Task.FromResult(new RpcConnection(channel, options));
}
