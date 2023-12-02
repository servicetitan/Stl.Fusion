using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public sealed class RpcPeerStateMonitor : WorkerBase
{
    private readonly IMutableState<RpcPeerState?> _state;
    private ILogger? _log;

    private IServiceProvider Services { get; }
    private ILogger Log => _log ??= Services.LogFor(GetType());

    public RpcPeerRef PeerRef { get; set; } = RpcPeerRef.Default;
    public TimeSpan StartDelay { get; set; } = TimeSpan.FromSeconds(1);
    public IState<RpcPeerState?> State => _state;

    public RpcPeerStateMonitor(IServiceProvider services)
    {
        Services = services;
        _state = services.StateFactory().NewMutable((RpcPeerState?)null, $"{GetType().Name}.{nameof(State)}");
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var hub = Services.RpcHub();
        while (true) {
            await Task.Delay(StartDelay, cancellationToken).ConfigureAwait(false);
            Log.LogInformation("`{PeerRef}`: monitor (re)started", PeerRef);
            var peer = hub.GetClientPeer(PeerRef);
            var peerCts = cancellationToken.LinkWith(peer.StopToken);
            var peerCancellationToken = peerCts.Token;
            try {
                // This delay gives some time for peer to connect
                while (true) {
                    peerCancellationToken.ThrowIfCancellationRequested();
                    var connectionState = peer.ConnectionState;
                    var isConnected = connectionState.Value.IsConnected();
                    var nextConnectionStateTask = connectionState.WhenNext(peerCancellationToken);

                    if (isConnected) {
                        _state.Value = new RpcPeerState(true);
                        await nextConnectionStateTask.ConfigureAwait(false);
                    }
                    else {
                        _state.Value = new RpcPeerState(false, connectionState.Value.Error);
                        // Disconnected -> update ReconnectsAt value until the nextConnectionStateTask completes
                        var stateChangedToken = CancellationTokenExt.FromTask(nextConnectionStateTask, CancellationToken.None);
                        try {
                            var reconnectAtChanges = peer.ReconnectsAt.Changes(stateChangedToken);
                            await foreach (var reconnectsAt in reconnectAtChanges.ConfigureAwait(false))
                                _state.Value = _state.Value with { ReconnectsAt = reconnectsAt };
                        }
                        catch (Exception e) when (e.IsCancellationOf(stateChangedToken)) {
                            // Intended
                        }
                    }
                }
            }
            catch (Exception e) {
                if (e.IsCancellationOf(cancellationToken)) {
                    Log.LogInformation("`{PeerRef}`: monitor stopped", PeerRef);
                    return;
                }
                if (peer.StopToken.IsCancellationRequested)
                    Log.LogWarning("`{PeerRef}`: peer is terminated, will restart", PeerRef);
                else
                    Log.LogError(e, "`{PeerRef}`: monitor failed, will restart", PeerRef);
            }
            finally {
                _state.Value = null;
                peerCts.CancelAndDisposeSilently();
            }
        }
    }
}
