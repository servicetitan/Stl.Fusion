using Stl.Fusion.Authentication;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Server.Rpc;

public class FusionSessionRpcMiddleware : RpcInboundMiddleware
{
    public FusionSessionRpcMiddleware(IServiceProvider services) : base(services) { }

    public override void BeforeCall(RpcInboundCall call)
    {
        var connection = call.Context.Peer.ConnectionState.Value.Connection as FusionRpcConnection;
        if (connection == null)
            return;

        var arguments = call.Arguments;
        var tItem0 = arguments!.GetType(0);
        if (tItem0 == typeof(Session)) {
            var session = arguments.Get<Session>(0);
            if (session.IsDefault()) {
                session = connection.Session.RequireValid();
                arguments.Set(0, session);
            }
            else
                session.RequireValid();
        }
        else if (typeof(ISessionCommand).IsAssignableFrom(tItem0)) {
            var command = arguments.Get<ISessionCommand>(0);
            var session = command.Session;
            if (session.IsDefault()) {
                command.SetSession(connection.Session.RequireValid());
                arguments.Set(0, command);
            }
            else
                session.RequireValid();
        }
    }
}
