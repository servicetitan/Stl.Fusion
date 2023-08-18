using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;

namespace Stl.Fusion.Authentication.Services;

public abstract class DbAuthService<TDbContext> : DbServiceBase<TDbContext>, IAuth, IAuthBackend
    where TDbContext : DbContext
{
    public record Options
    {
        // The default should be less than 3 min - see PresenceService.Options
        public TimeSpan MinUpdatePresencePeriod { get; init; } = TimeSpan.FromMinutes(2.75);
    }

    protected DbAuthService(IServiceProvider services) : base(services)
    { }

    // IAuth
    public abstract Task SignOut(Auth_SignOut command, CancellationToken cancellationToken = default);
    public abstract Task EditUser(Auth_EditUser command, CancellationToken cancellationToken = default);
    public abstract Task UpdatePresence(Session session, CancellationToken cancellationToken = default);
    public abstract Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
    public abstract Task<SessionAuthInfo?> GetAuthInfo(Session session, CancellationToken cancellationToken = default);
    public abstract Task<User?> GetUser(Session session, CancellationToken cancellationToken = default);
    public abstract Task<ImmutableArray<SessionInfo>> GetUserSessions(Session session, CancellationToken cancellationToken = default);

    // IAuthBackend
    public abstract Task SignIn(AuthBackend_SignIn command, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo> SetupSession(AuthBackend_SetupSession command, CancellationToken cancellationToken = default);
    public abstract Task SetOptions(Auth_SetSessionOptions command, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    public abstract Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default);
    public abstract Task<User?> GetUser(Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default);
}
