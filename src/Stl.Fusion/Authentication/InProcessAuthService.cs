using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    [ComputeService(typeof(IServerAuthService))]
    [ServiceAlias(typeof(IAuthService), typeof(IServerAuthService))]
    public class InProcessAuthService : IServerAuthService
    {
        protected ConcurrentDictionary<string, AuthUser> Sessions { get; } =
            new ConcurrentDictionary<string, AuthUser>();
        protected IAuthSessionAccessor AuthSessionAccessor { get; }

        public InProcessAuthService(IAuthSessionAccessor authSessionAccessor)
            => AuthSessionAccessor = authSessionAccessor;

        public Task LoginAsync(
            AuthUser user, AuthSession? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= AuthSessionAccessor.Session ?? throw new ArgumentNullException(nameof(session));
            Sessions[session.Id] = user;
            Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public Task LogoutAsync(
            AuthSession? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= AuthSessionAccessor.Session ?? throw new ArgumentNullException(nameof(session));
            if (Sessions.TryRemove(session.Id, out var _))
                Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public virtual Task<AuthUser> GetUserAsync(
            AuthSession? session = null,
            CancellationToken cancellationToken = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            var user = Sessions.GetValueOrDefault(session.Id) ?? new AuthUser(session.Id);
            return Task.FromResult(user)!;
        }
    }
}
