using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;

namespace Templates.Blazor2.UI.Services
{
    [Service(Lifetime = ServiceLifetime.Scoped)]
    public class ClientState : IDisposable
    {
        protected AuthStateProvider AuthStateProvider { get; }
        protected ISessionResolver SessionResolver { get; }

        // Handy shortcuts
        public Session Session => SessionResolver.Session;
        public ILiveState<AuthState> AuthState => AuthStateProvider.State;
        // Own properties
        public ILiveState<User> User { get; }

        public ClientState(AuthStateProvider authStateProvider, IStateFactory stateFactory)
        {
            AuthStateProvider = authStateProvider;
            SessionResolver = AuthStateProvider.SessionResolver;

            User = stateFactory.NewLive<User>(
                o => o.WithUpdateDelayer(0, 1),
                async (_, cancellationToken) => {
                    var authState = await AuthState.UseAsync(cancellationToken).ConfigureAwait(false);
                    return authState.User;
                });
        }

        void IDisposable.Dispose() => User.Dispose();
    }
}
