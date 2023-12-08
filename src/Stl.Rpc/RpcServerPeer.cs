using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public class RpcServerPeer(RpcHub hub, RpcPeerRef @ref) : RpcPeer(hub, @ref)
{
    private volatile AsyncState<RpcConnection?> _nextConnection = new(null, true);

    public TimeSpan CloseTimeout { get; init; } = TimeSpan.FromMinutes(10);

    public void SetConnection(RpcConnection connection)
    {
        AsyncState<RpcPeerConnectionState> connectionState;
        lock (Lock) {
            connectionState = ConnectionState;
            if (connectionState.Value.Connection == connection)
                return; // Already using connection
            if (_nextConnection.Value == connection)
                return; // Already "scheduled" to use connection

            _nextConnection = _nextConnection.SetNext(connection);
        }
        _ = Disconnect(true, null, connectionState);
    }

    // Protected methods

    protected override async Task<RpcConnection> GetConnection(
        RpcPeerConnectionState connectionState,
        CancellationToken cancellationToken)
    {
        while (true) {
            AsyncState<RpcConnection?> nextConnection;
            lock (Lock) {
                nextConnection = _nextConnection;
                var connection = nextConnection.Value;
                if (connection != null) {
                    _nextConnection = nextConnection.SetNext(null); // This allows SetConnection to work properly
                    return connection;
                }
            }
            try {
                await nextConnection
                    .When(x => x != null, cancellationToken)
                    .WaitAsync(CloseTimeout, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException e) {
                throw Errors.ConnectionUnrecoverable(e);
            }
        }
    }
}
