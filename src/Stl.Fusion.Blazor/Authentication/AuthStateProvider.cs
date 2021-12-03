using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor.Internal;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor;

public class AuthStateProvider : AuthenticationStateProvider, IDisposable
{
    public class Options
    {
        public Action<ComputedState<AuthState>.Options> AuthStateOptionsBuilder { get; } =
            DefaultAuthStateOptionsBuilder;

        public static void DefaultAuthStateOptionsBuilder(ComputedState<AuthState>.Options options)
            => options.UpdateDelayer =
                UpdateDelayer.MinDelay with {
                    MaxRetryDelayDuration = TimeSpan.FromSeconds(10),
                };
    }

    private volatile IStateSnapshot<AuthState>? _cachedStateSnapshot;
    private volatile Task<AuthenticationState>? _cachedStateValueTask;

    // These properties are intentionally public -
    // e.g. State is quite handy to consume in other compute methods or states
    public ISessionResolver SessionResolver { get; }
    public IAuth Auth { get; }
    public IUICommandTracker UICommandTracker { get; }
    public IComputedState<AuthState> State { get; }

    public AuthStateProvider(
        Options? options,
        ISessionResolver sessionResolver,
        IAuth auth,
        IUICommandTracker uiCommandTracker,
        IStateFactory stateFactory)
    {
        options ??= new();
        SessionResolver = sessionResolver;
        Auth = auth;
        UICommandTracker = uiCommandTracker;
        State = stateFactory.NewComputed<AuthState>(o => {
            options.AuthStateOptionsBuilder(o);
            o.InitialOutputFactory = _ => new AuthState(new User("none"));
            o.EventConfigurator += state => state.AddEventHandler(StateEventKind.Updated, OnStateChanged);
        }, ComputeState);
    }

    public void Dispose()
        => State.Dispose();

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Simplest version of this method:
        // return State.Update().AsTask().ContinueWith(t => (AuthenticationState) t.Result.LatestNonErrorValue);

        // More performant version relying on task caching:
        if (_cachedStateValueTask != null) {
            if (!_cachedStateValueTask.IsCompleted)
                return _cachedStateValueTask;
            var snapshot = State.Snapshot;
            if (_cachedStateSnapshot == snapshot && snapshot.Computed.IsConsistent())
                return _cachedStateValueTask;
        }

        _cachedStateValueTask = State.Update().AsTask().ContinueWith(t => {
            var snapshot = t.Result.Snapshot;
            _cachedStateSnapshot = snapshot;
            return (AuthenticationState) snapshot.LatestNonErrorComputed.Value;
        }, TaskScheduler.Current);
        return _cachedStateValueTask;
    }

    protected virtual async Task<AuthState> ComputeState(IComputedState<AuthState> state, CancellationToken cancellationToken)
    {
        var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
        var user = await Auth.GetSessionUser(session, cancellationToken).ConfigureAwait(false);
        // AuthService.GetUser checks for forced sign-out as well, so
        // we should explicitly query its state for unauthenticated users only
        var isSignOutForced = !user.IsAuthenticated
            && await Auth.IsSignOutForced(session, cancellationToken).ConfigureAwait(false);
        return new AuthState(user, isSignOutForced);
    }

    protected virtual void OnStateChanged(IState<AuthState> state, StateEventKind eventKind)
    {
        using var suppressing = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => {
            var authStateTask = Task.FromResult((AuthenticationState) state.LatestNonErrorValue);
            NotifyAuthenticationStateChanged(authStateTask);
            var startedEvent = new UICommandEvent(new ChangeAuthStateUICommand());
            UICommandTracker.ProcessEvent(startedEvent);
            var completedEvent = new UICommandEvent(startedEvent.Command, Result.New(state));
            UICommandTracker.ProcessEvent(completedEvent);
        });
    }
}
