using Stl.Rpc;

namespace Stl.Fusion.Extensions;

public class RpcPeerStateMonitor : WorkerBase
{
    private IMutableState<RpcPeerState> _peerState = null!;
    private ILogger? _log;

    protected IServiceProvider Services => RpcHub.Services;
    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected Moment Now => RpcHub.Clock.Now;

    public RpcHub RpcHub { get; }
    public RpcPeerRef? PeerRef { get; }
    public TimeSpan JustDisconnectedPeriod { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MinReconnectsIn { get; init; } = TimeSpan.FromSeconds(1);

    public IState<RpcPeerState> PeerState {
        get => _peerState;
        protected set => _peerState = (IMutableState<RpcPeerState>)value;
    }
    public IState<Moment> LastReconnectDelayCancelledAt { get; protected set; } = null!;
    public IState<RpcPeerComputedState> State { get; protected set; } = null!;

    public RpcPeerStateMonitor(
        IServiceProvider services,
        RpcPeerRef? peerRef,
        bool mustStart = true,
        bool mustCreateStates = true)
    {
        RpcHub = services.RpcHub();
        PeerRef = peerRef;
        if (!mustCreateStates)
            return;

        var connectionState = peerRef == null ? null : RpcHub.GetPeer(peerRef).ConnectionState.Value;
        var isConnected = connectionState?.IsConnected() ?? true;
        RpcPeerState initialPeerState = isConnected
            ? new RpcPeerConnectedState(Now)
            : new RpcPeerDisconnectedState(Now, default, connectionState?.Error);

        var stateFactory = services.StateFactory();
        _peerState = stateFactory.NewMutable(
            initialPeerState,
            $"{GetType().Name}.{nameof(PeerState)}");
        var stateCategory = $"{GetType().Name}.{nameof(LastReconnectDelayCancelledAt)}";
        LastReconnectDelayCancelledAt = peerRef == null
            ? stateFactory.NewMutable((Moment)default, stateCategory)
            : stateFactory.NewComputed<Moment>(
                FixedDelayer.Instant,
                ComputeLastReconnectDelayCancelledAtState,
                stateCategory);
        stateCategory = $"{GetType().Name}.{nameof(State)}";
        State = peerRef == null
            ? stateFactory.NewMutable(new RpcPeerComputedState(RpcPeerComputedStateKind.Connected), stateCategory)
            : stateFactory.NewComputed<RpcPeerComputedState>(FixedDelayer.Instant, ComputeState, stateCategory);
        if (mustStart)
            Start();
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        if (State is IDisposable d1)
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
                        _peerState.Value = _peerState.Value.ToConnected(Now);
                        Log.LogInformation("`{PeerRef}`: state = {State}", peerRef, _peerState.Value);
                        await nextConnectionStateTask.ConfigureAwait(false);
                    }
                    else {
                        var state = _peerState.Value.ToDisconnected(Now, peer.ReconnectsAt.Value, connectionState.Value);
                        _peerState.Value = state;
                        Log.LogInformation("`{PeerRef}`: state = {State}", peerRef, state);
                        // Disconnected -> update ReconnectsAt value until the nextConnectionStateTask completes
                        var stateChangedToken = CancellationTokenExt.FromTask(nextConnectionStateTask, CancellationToken.None);
                        try {
                            var reconnectAtChanges = peer.ReconnectsAt.Changes(stateChangedToken);
                            await foreach (var reconnectsAt in reconnectAtChanges.ConfigureAwait(false)) {
                                if (state.ReconnectsAt != reconnectsAt) {
                                    _peerState.Value = state = state with { ReconnectsAt = reconnectsAt };
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
                _peerState.Value = _peerState.Value.ToDisconnected(Now, default, error);
                peerCts.CancelAndDisposeSilently();
            }
        }
    }

    protected virtual Task<Moment> ComputeLastReconnectDelayCancelledAtState(
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

    protected virtual async Task<RpcPeerComputedState> ComputeState(
        IComputedState<RpcPeerComputedState> state, CancellationToken cancellationToken)
    {
        var s = await PeerState.Use(cancellationToken).ConfigureAwait(false);
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
