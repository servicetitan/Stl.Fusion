using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Stl.Async;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor
{
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
        public IAuthService AuthService { get; }
        public IComputedState<AuthState> State { get; }

        public AuthStateProvider(
            Options? options,
            IAuthService authService,
            ISessionResolver sessionResolver,
            IStateFactory stateFactory)
        {
            options ??= new();
            AuthService = authService;
            SessionResolver = sessionResolver;
            State = stateFactory.NewComputed<AuthState>(o => {
                options.AuthStateOptionsBuilder.Invoke(o);
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
            });
            return _cachedStateValueTask;
        }

        protected virtual async Task<AuthState> ComputeState(IComputedState<AuthState> state, CancellationToken cancellationToken)
        {
            var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
            var user = await AuthService.GetUser(session, cancellationToken).ConfigureAwait(false);
            // AuthService.GetUser checks for forced sign-out as well, so
            // we should explicitly query its state for unauthenticated users only
            var isSignOutForced = !user.IsAuthenticated
                && await AuthService.IsSignOutForced(session, cancellationToken).ConfigureAwait(false);
            return new AuthState(user, isSignOutForced);
        }

        protected virtual void OnStateChanged(IState<AuthState> state, StateEventKind eventKind)
        {
            using var suppressing = ExecutionContextEx.SuppressFlow();
            _ = Task.Run(() => {
                var authStateTask = Task.FromResult((AuthenticationState) state.LatestNonErrorValue);
                NotifyAuthenticationStateChanged(authStateTask);
            });
        }
    }
}
