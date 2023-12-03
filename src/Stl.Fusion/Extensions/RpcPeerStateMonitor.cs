using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public sealed class RpcPeerStateMonitor : WorkerBase
{
    private readonly IMutableState<RpcPeerState> _state;
    private ILogger? _log;

    private IServiceProvider Services => RpcHub.Services;
    private ILogger Log => _log ??= Services.LogFor(GetType());
    private Moment Now => RpcHub.Clock.Now;

    public RpcHub RpcHub { get; }
    public RpcPeerRef? PeerRef { get; }
    public TimeSpan JustDisconnectedPeriod { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MinReconnectsIn { get; init; } = TimeSpan.FromSeconds(1);

    public IState<RpcPeerState> State => _state;
    public IState<Moment> LastReconnectDelayCancelledAt { get; }
    public IState<RpcPeerComputedState> ComputedState { get; }

    public RpcPeerStateMonitor(IServiceProvider services, RpcPeerRef? peerRef, bool mustStart = true)
    {
        RpcHub = services.RpcHub();
        PeerRef = peerRef;
        var connectionState = peerRef == null ? null : RpcHub.GetPeer(peerRef).ConnectionState.Value;
        var isConnected = connectionState?.IsConnected() ?? true;
        RpcPeerState initialState = isConnected
            ? new RpcPeerConnectedState(Now)
            : new RpcPeerDisconnectedState(Now, default, connectionState?.Error);

        var stateFactory = services.StateFactory();
        _state = stateFactory.NewMutable(
            initialState,
            $"{GetType().Name}.{nameof(State)}");
        var stateCategory = $"{GetType().Name}.{nameof(LastReconnectDelayCancelledAt)}";
        LastReconnectDelayCancelledAt = peerRef == null
            ? stateFactory.NewMutable((Moment)default, stateCategory)
            : stateFactory.NewComputed<Moment>(
                FixedDelayer.Instant,
                ComputeLastReconnectDelayCancelledAtState,
                stateCategory);
        stateCategory = $"{GetType().Name}.{nameof(ComputedState)}";
        ComputedState = peerRef == null
            ? stateFactory.NewMutable(new RpcPeerComputedState(RpcPeerComputedStateKind.Connected), stateCategory)
            : stateFactory.NewComputed<RpcPeerComputedState>(FixedDelayer.Instant, ComputeComputedState, stateCategory);
        if (mustStart)
            Start();
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        if (ComputedState is IDisposable d1)
            d1.Dispose();
        if (LastReconnectDelayCancelledAt is IDisposable d2)
            d2.Dispose();
    }

    public void Start()
    {
        if (PeerRef != null)
            _ = Run();
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var peerRef = PeerRef;
        if (peerRef == null) // Always connected
            return;

        while (true) {
            Log.LogInformation("`{PeerRef}`: monitor (re)started", peerRef);
            var peer = RpcHub.GetClientPeer(peerRef);
            var peerCts = cancellationToken.LinkWith(peer.StopToken);
            var peerCancellationToken = peerCts.Token;
            var error = (Exception?)null;
            try {
                // This delay gives some time for peer to connect
                while (true) {
                    peerCancellationToken.ThrowIfCancellationRequested();
                    var connectionState = peer.ConnectionState;
                    var isConnected = connectionState.Value.IsConnected();
                    var nextConnectionStateTask = connectionState.WhenNext(peerCancellationToken);

                    if (isConnected) {
                        _state.Value = _state.Value.ToConnected(Now);
                        Log.LogInformation("`{PeerRef}`: state = {State}", peerRef, _state.Value);
                        await nextConnectionStateTask.ConfigureAwait(false);
                    }
                    else {
                        var state = _state.Value.ToDisconnected(Now, peer.ReconnectsAt.Value, connectionState.Value);
                        _state.Value = state;
                        Log.LogInformation("`{PeerRef}`: state = {State}", peerRef, state);
                        // Disconnected -> update ReconnectsAt value until the nextConnectionStateTask completes
                        var stateChangedToken = CancellationTokenExt.FromTask(nextConnectionStateTask, CancellationToken.None);
                        try {
                            var reconnectAtChanges = peer.ReconnectsAt.Changes(stateChangedToken);
                            await foreach (var reconnectsAt in reconnectAtChanges.ConfigureAwait(false)) {
                                if (state.ReconnectsAt != reconnectsAt) {
                                    _state.Value = state = state with { ReconnectsAt = reconnectsAt };
                                    Log.LogInformation("`{PeerRef}`: state = {State}", peerRef, state);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (stateChangedToken.IsCancellationRequested) {
                            // Intended
                        }
                    }
                }
            }
            catch (Exception e) {
                if (e.IsCancellationOf(cancellationToken)) {
                    Log.LogInformation("`{PeerRef}`: monitor stopped", peerRef);
                    return;
                }

                error = e;
                if (peer.StopToken.IsCancellationRequested)
                    Log.LogWarning("`{PeerRef}`: peer is terminated, will restart", peerRef);
                else
                    Log.LogError(e, "`{PeerRef}`: monitor failed, will restart", peerRef);
            }
            finally {
                _state.Value = _state.Value.ToDisconnected(Now, default, error);
                peerCts.CancelAndDisposeSilently();
            }
        }
    }

    private Task<Moment> ComputeLastReconnectDelayCancelledAtState(
        IComputedState<Moment> state, CancellationToken cancellationToken)
    {
        var reconnectDelayer = RpcHub.InternalServices.ClientPeerReconnectDelayer;
        var computed = Computed.GetCurrent();
        reconnectDelayer.CancelDelaysToken.Register(static c => {
            // It makes sense to wait a bit after the cancellation to let RpcPeer do some work
            _ = Task.Delay(50, CancellationToken.None).ContinueWith(
                _ => (c as IComputed)?.Invalidate(),
                TaskScheduler.Default);
        }, computed);
        return Task.FromResult(Now);
    }

    private async Task<RpcPeerComputedState> ComputeComputedState(
        IComputedState<RpcPeerComputedState> state, CancellationToken cancellationToken)
    {
        var s = await State.Use(cancellationToken).ConfigureAwait(false);
        if (s is not RpcPeerDisconnectedState d)
            return new RpcPeerComputedState(RpcPeerComputedStateKind.Connected);

        var disconnectedFor = Now - d.DisconnectedAt;
        if (disconnectedFor < JustDisconnectedPeriod) {
            var invalidateIn = JustDisconnectedPeriod + TimeSpan.FromMilliseconds(250) - disconnectedFor;
            Computed.GetCurrent()!.Invalidate(invalidateIn, false);
            return new RpcPeerComputedState(RpcPeerComputedStateKind.JustDisconnected, d.LastError);
        }
        var reconnectsIn = d.ReconnectsAt - Now;
        if (reconnectsIn < MinReconnectsIn)
            return new RpcPeerComputedState(RpcPeerComputedStateKind.Reconnecting, d.LastError);

        // Just to create a dependency that will trigger the recompute
        await LastReconnectDelayCancelledAt.Use(cancellationToken).ConfigureAwait(false);
        return new RpcPeerComputedState(RpcPeerComputedStateKind.Disconnected, d.LastError, reconnectsIn);
    }
}
