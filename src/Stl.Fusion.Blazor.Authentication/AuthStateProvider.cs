using Microsoft.AspNetCore.Components.Authorization;
using Stl.Fusion.Authentication;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor.Authentication;

public class AuthStateProvider : AuthenticationStateProvider, IDisposable
{
    public record Options
    {
        public IUpdateDelayer UpdateDelayer { get; init; }

        public Options()
        {
            var updateDelayer = FixedDelayer.Instant;
            UpdateDelayer = updateDelayer with {
                RetryDelays = updateDelayer.RetryDelays with { Max = TimeSpan.FromSeconds(10) },
            };
        }
    };

    private Session? _session;
    private volatile IStateSnapshot<AuthState>? _cachedStateSnapshot;
    private volatile Task<AuthenticationState>? _cachedStateValueTask;

    // These properties are intentionally public -
    // e.g. State is quite handy to consume in other compute methods or states
    public IComputedState<AuthState> State { get; }

    protected IServiceProvider Services { get; }
    protected IAuth Auth { get; }
    protected ISessionResolver SessionResolver { get; }
    protected UIActionTracker UIActionTracker { get; }

    public AuthStateProvider(Options settings, IServiceProvider services)
    {
        Services = services;
        SessionResolver = services.GetRequiredService<ISessionResolver>();
        Auth = services.GetRequiredService<IAuth>();
        UIActionTracker = services.UIActionTracker();

        // ReSharper disable once VirtualMemberCallInConstructor
#pragma warning disable CA2214
        var stateOptions = GetStateOptions(settings);
#pragma warning restore CA2214
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
            return (AuthenticationState) snapshot.LastNonErrorComputed.Value;
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
        return _cachedStateValueTask;
    }

    protected virtual ComputedState<AuthState>.Options GetStateOptions(Options settings)
        => new() {
            InitialValue = new(),
            UpdateDelayer = settings.UpdateDelayer,
            EventConfigurator = state => state.AddEventHandler(StateEventKind.Updated, OnStateChanged),
            MustFlowExecutionContext = true, // To preserve current culture
        };

    protected virtual async Task<AuthState> ComputeState(IComputedState<AuthState> state, CancellationToken cancellationToken)
    {
        // We have to use ISessionResolver.GetSession() here
        _session ??= await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
        var user = await Auth.GetUser(_session, cancellationToken).ConfigureAwait(false);
        // AuthService.GetUser checks for forced sign-out as well, so
        // we should explicitly query its state for unauthenticated users only
        var isSignOutForced = user == null
            && await Auth.IsSignOutForced(_session, cancellationToken).ConfigureAwait(false);
        return new AuthState(user, isSignOutForced);
    }

    protected virtual void OnStateChanged(IState<AuthState> state, StateEventKind eventKind)
        => _ = Task.Run(() => {
            var authenticationStateTask = Task.FromResult((AuthenticationState)state.LastNonErrorValue);
            NotifyAuthenticationStateChanged(authenticationStateTask);

            var authStateTask = Task.FromResult(state.LastNonErrorValue);
            var clock = UIActionTracker.Clock;
            var action = new UIAction<AuthState>(new ChangeAuthStateUICommand(), clock, authStateTask, default);
            UIActionTracker.Register(action);
        });
}
