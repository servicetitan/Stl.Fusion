using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    [ComputeService(typeof(IServerAuthenticator))]
    [ServiceAlias(typeof(IAuthenticator), typeof(IServerAuthenticator))]
    public class InProcessServerAuthenticator : IServerAuthenticator
    {
        protected ConcurrentDictionary<string, Principal> Sessions { get; } =
            new ConcurrentDictionary<string, Principal>();
        protected ISessionAccessor SessionAccessor { get; }

        public InProcessServerAuthenticator(ISessionAccessor sessionAccessor)
            => SessionAccessor = sessionAccessor;

        public Task LoginAsync(Principal user, Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionAccessor.Session ?? throw new ArgumentNullException(nameof(session));
            Sessions[session.Id] = user;
            Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public Task LogoutAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionAccessor.Session ?? throw new ArgumentNullException(nameof(session));
            if (Sessions.TryRemove(session.Id, out var _))
                Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public virtual Task<Principal> GetUserAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            var user = Sessions.GetValueOrDefault(session.Id) ?? new Principal(session.Id);
            return Task.FromResult(user)!;
        }
    }
}
