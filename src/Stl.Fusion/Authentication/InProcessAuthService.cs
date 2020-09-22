using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public class InProcessAuthService : IServerSideAuthService
    {
        protected ConcurrentDictionary<string, AuthUser> Sessions { get; } =
            new ConcurrentDictionary<string, AuthUser>();

        public Task LoginAsync(
            AuthUser user, AuthContext? context = null,
            CancellationToken cancellationToken = default)
        {
            context ??= AuthContext.Current.AssertNotNull();
            Sessions[context.Id] = user;
            Computed.Invalidate(() => GetUserAsync(context, default));
            return Task.CompletedTask;
        }

        public Task LogoutAsync(
            AuthContext? context = null,
            CancellationToken cancellationToken = default)
        {
            context ??= AuthContext.Current.AssertNotNull();
            if (Sessions.TryRemove(context.Id, out var _))
                Computed.Invalidate(() => GetUserAsync(context, default));
            return Task.CompletedTask;
        }

        public virtual Task<AuthUser> GetUserAsync(
            AuthContext? context = null,
            CancellationToken cancellationToken = default)
        {
            context ??= AuthContext.Current.AssertNotNull();
            var user = Sessions.GetValueOrDefault(context.Id) ?? new AuthUser(context.Id);
            return Task.FromResult(user)!;
        }
    }
}
