using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Stl.Async;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor.Authentication
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
        protected Task<AuthContext> AuthContextTask { get; }
        protected ILiveState<AuthState> State { get; }

        public AuthStateProvider(
            IAuthService authService,
            Task<AuthContext> authContextTask,
            IStateFactory stateFactory)
            : this(null, authService, authContextTask, stateFactory) { }
        public AuthStateProvider(
            Options? options,
            IAuthService authService,
            Task<AuthContext> authContextTask,
            IStateFactory stateFactory)
        {
            options ??= new Options();
            AuthService = authService;
            AuthContextTask = authContextTask;
            State = stateFactory.NewLive<AuthState>(o => {
                options.LiveStateOptionsBuilder.Invoke(o);
                o.InitialOutputFactory = _ => new AuthState(new AuthUser(""));
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
            var context = await AuthContextTask.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
            var user = await AuthService.GetUserAsync(context, cancellationToken).ConfigureAwait(false);
            return new AuthState(user);
        }

        protected virtual void OnStateInvalidated(IState<AuthState> obj) { }

        protected virtual void OnStateUpdated(IState<AuthState> state)
            => NotifyAuthenticationStateChanged(Task.FromResult((AuthenticationState) state.LastValue));
    }
}
