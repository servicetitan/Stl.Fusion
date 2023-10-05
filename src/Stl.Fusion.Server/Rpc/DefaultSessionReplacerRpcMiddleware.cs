using Stl.Fusion.Authentication;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class DefaultSessionReplacerRpcMiddleware(IServiceProvider services)
    : RpcInboundMiddleware(services)
{
    public override Task OnBeforeCall(RpcInboundCall call)
    {
        var connection = call.Context.Peer.ConnectionState.Value.Connection as SessionBoundRpcConnection;
        if (connection == null)
            return Task.CompletedTask;

        var arguments = call.Arguments;
        var tItem0 = arguments!.GetType(0);
        if (tItem0 == typeof(Session)) {
            var session = arguments.Get<Session>(0);
            if (session.IsDefault()) {
                session = connection.Session;
                arguments.Set(0, session);
            }
            else
                session.RequireValid();
        }
        else if (typeof(ISessionCommand).IsAssignableFrom(tItem0)) {
            var command = arguments.Get<ISessionCommand>(0);
            var session = command.Session;
            if (session.IsDefault())
                command.SetSession(connection.Session);
            else
                session.RequireValid();
        }
        return Task.CompletedTask;
    }
}
