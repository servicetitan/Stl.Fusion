using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.EntityFramework.Authentication;

public abstract class DbAuthService<TDbContext> : DbServiceBase<TDbContext>, IAuth, IAuthBackend
    where TDbContext : DbContext
{
    public class Options
    {
        // The default should be less than 3 min - see PresenceService.Options
        public TimeSpan MinUpdatePresencePeriod { get; set; } = TimeSpan.FromMinutes(2.75);
    }

    protected DbAuthService(IServiceProvider services) : base(services) { }

    // IAuth
    public abstract Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default);
    public abstract Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default);
    public abstract Task UpdatePresence(Session session, CancellationToken cancellationToken = default);
    public abstract Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
    public abstract Task<SessionAuthInfo> GetAuthInfo(Session session, CancellationToken cancellationToken = default);
    public abstract Task<User> GetUser(Session session, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default);

    // IAuthBackend
    public abstract Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
    public abstract Task SetOptions(SetSessionOptionsCommand command, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    public abstract Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default);
    public abstract Task<User?> GetUser(string userId, CancellationToken cancellationToken = default);
}
