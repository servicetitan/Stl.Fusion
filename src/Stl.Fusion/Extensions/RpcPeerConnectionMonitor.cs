using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public sealed class RpcPeerConnectionMonitor : WorkerBase
{
    private readonly IMutableState<bool?> _isConnected;

    private IServiceProvider Services { get; }

    public RpcPeerRef PeerRef { get; set; } = RpcPeerRef.Default;
    public TimeSpan StartDelay { get; set; } = TimeSpan.FromSeconds(1);
    public IState<bool?> IsConnected => _isConnected;

    public RpcPeerConnectionMonitor(IServiceProvider services)
    {
        Services = services;
        _isConnected = services.StateFactory().NewMutable((bool?)null, $"{GetType().Name}.{nameof(IsConnected)}");
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        try {
            var peer = Services.RpcHub().GetPeer(PeerRef);
            // This delay gives some time for peer to connect
            await Task.Delay(StartDelay, cancellationToken).ConfigureAwait(false);
            await foreach (var state in peer.ConnectionState.Changes(cancellationToken).ConfigureAwait(false)) {
                if (state.Error is { } error)
                    _isConnected.Error = error;
                else
                    _isConnected.Value = state.IsConnected();
            }
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            _isConnected.Error = e;
        }
    }
}
