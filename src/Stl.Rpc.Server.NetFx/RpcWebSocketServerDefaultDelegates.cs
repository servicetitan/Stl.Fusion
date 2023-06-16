using Microsoft.Owin;

namespace Stl.Rpc.Server;

public delegate RpcPeerRef RpcWebSocketServerPeerRefFactory(RpcWebSocketServer server, IOwinContext context);

public static class RpcWebSocketServerDefaultDelegates
{
    public static RpcWebSocketServerPeerRefFactory PeerRefFactory { get; set; } =
        static (server, context) => {
            var query = context.Request.Query;
            var clientId = query[server.Settings.ClientIdParameterName];
            return RpcPeerRef.NewServer(clientId);
        };
}
