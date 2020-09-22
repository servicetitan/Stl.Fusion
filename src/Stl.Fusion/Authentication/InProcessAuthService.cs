using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public class InProcessAuthService : IServerSideAuthService
    {
        protected ConcurrentDictionary<string, User> Sessions { get; } =
            new ConcurrentDictionary<string, User>();

        public Task LoginAsync(
            User user, Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            Sessions[session.Id] = user;
            Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public Task LogoutAsync(
            Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            if (Sessions.TryRemove(session.Id, out var _))
                Computed.Invalidate(() => GetUserAsync(session, default));
            return Task.CompletedTask;
        }

        public virtual Task<User> GetUserAsync(
            Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= Session.Current.AssertNotNull();
            var user = Sessions.GetValueOrDefault(session.Id) ?? new User(session.Id);
            return Task.FromResult(user)!;
        }
    }
}
