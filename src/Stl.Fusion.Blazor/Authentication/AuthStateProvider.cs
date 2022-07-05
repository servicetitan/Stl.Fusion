using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor.Internal;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor;

public class AuthStateProvider : AuthenticationStateProvider, IDisposable
{
    public record Options
    {
        public IUpdateDelayer UpdateDelayer { get; init; }

        public Options()
        {
            var minDelayUpdateDelayer = Fusion.UpdateDelayer.MinDelay;
            UpdateDelayer = minDelayUpdateDelayer with {
                RetryDelays = minDelayUpdateDelayer.RetryDelays with { Max = TimeSpan.FromSeconds(10) },
            };
        }
    };

    private volatile IStateSnapshot<AuthState>? _cachedStateSnapshot;
    private volatile Task<AuthenticationState>? _cachedStateValueTask;

    // These properties are intentionally public -
    // e.g. State is quite handy to consume in other compute methods or states
    public Session Session { get; }
    public IComputedState<AuthState> State { get; }

    protected IServiceProvider Services { get; }
    protected IAuth Auth { get; }
    protected IUICommandTracker UICommandTracker { get; }

    public AuthStateProvider(Options options, IServiceProvider services)
    {
        Services = services;
        Auth = services.GetRequiredService<IAuth>();
        UICommandTracker = services.UICommandTracker();

        Session = services.GetRequiredService<Session>();
        // ReSharper disable once VirtualMemberCallInConstructor
        var stateOptions = GetStateOptions(options);
        State = services.StateFactory().NewComputed(stateOptions, ComputeState);
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

    protected virtual ComputedState<AuthState>.Options GetStateOptions(Options options)
        => new() {
            InitialValue = new(null, Session.Default),
            UpdateDelayer = options.UpdateDelayer,
            EventConfigurator = state => state.AddEventHandler(StateEventKind.Updated, OnStateChanged),
        };

    protected virtual async Task<AuthState> ComputeState(IComputedState<AuthState> state, CancellationToken cancellationToken)
    {
        var user = await Auth.GetUser(Session, cancellationToken).ConfigureAwait(false);
        // AuthService.GetUser checks for forced sign-out as well, so
        // we should explicitly query its state for unauthenticated users only
        var isSignOutForced = user == null
            && await Auth.IsSignOutForced(Session, cancellationToken).ConfigureAwait(false);
        return new AuthState(user, Session, isSignOutForced);
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
