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
                    UpdateDelayer.MinUpdateDelay with {
                        MaxRetryDelayDuration = TimeSpan.FromSeconds(10),
                    };
        }

        // These properties are intentionally public -
        // e.g. State is quite handy to consume in other compute methods or states
        public ISessionResolver SessionResolver { get; }
        public IAuthService AuthService { get; }
        public IComputedState<AuthState> AuthState { get; }

        public AuthStateProvider(
            Options? options,
            IAuthService authService,
            ISessionResolver sessionResolver,
            IStateFactory stateFactory)
        {
            options ??= new();
            AuthService = authService;
            SessionResolver = sessionResolver;
            AuthState = stateFactory.NewComputed<AuthState>(o => {
                options.AuthStateOptionsBuilder.Invoke(o);
                o.InitialOutputFactory = _ => new AuthState(new User("none"));
                o.EventConfigurator += state => state.AddEventHandler(StateEventKind.Updated, OnStateChanged);
            }, ComputeState);
        }

        public void Dispose() => AuthState.Dispose();
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var state = await AuthState.Update().ConfigureAwait(false);
            return state.LatestNonErrorValue;
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
            using var _ = ExecutionContextEx.SuppressFlow();
            Task.Run(() => {
                var authStateTask = Task.FromResult((AuthenticationState) state.LatestNonErrorValue);
                NotifyAuthenticationStateChanged(authStateTask);
            }).Ignore();
        }
    }
}
