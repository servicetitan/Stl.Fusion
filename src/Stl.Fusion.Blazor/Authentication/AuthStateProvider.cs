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
            public Action<LiveState<AuthState>.Options> LiveStateOptionsBuilder { get; } =
                DefaultLiveStateOptionsBuilder;

            public static void DefaultLiveStateOptionsBuilder(LiveState<AuthState>.Options options)
                => options.WithUpdateDelayer(0.1, 10);
        }

        protected IAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected ILiveState<AuthState> State { get; }

        public AuthStateProvider(
            IAuthService authService,
            ISessionResolver sessionResolver,
            IStateFactory stateFactory)
            : this(null, authService, sessionResolver, stateFactory) { }
        public AuthStateProvider(
            Options? options,
            IAuthService authService,
            ISessionResolver sessionResolver,
            IStateFactory stateFactory)
        {
            options ??= new Options();
            AuthService = authService;
            SessionResolver = sessionResolver;
            State = stateFactory.NewLive<AuthState>(o => {
                options.LiveStateOptionsBuilder.Invoke(o);
                o.InitialOutputFactory = _ => new AuthState(new User(""));
                o.Invalidated += OnStateInvalidated;
                o.Updated += OnStateUpdated;
            }, ComputeState);
        }

        public void Dispose() => State.Dispose();
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var state = await State.UpdateAsync(false).ConfigureAwait(false);
            return state.LastValue;
        }

        protected virtual async Task<AuthState> ComputeState(ILiveState<AuthState> state, CancellationToken cancellationToken)
        {
            var session = await SessionResolver.GetSessionAsync(cancellationToken).ConfigureAwait(false);
            var user = await AuthService.GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            return new AuthState(user);
        }

        protected virtual void OnStateInvalidated(IState<AuthState> obj) { }

        protected virtual void OnStateUpdated(IState<AuthState> state)
            => NotifyAuthenticationStateChanged(Task.FromResult((AuthenticationState) state.LastValue));
    }
}
