using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;
using Stl.Multitenancy;

namespace Stl.Fusion.Authentication.Services;

public partial class DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> : DbAuthService<TDbContext>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected Options Settings { get; }
    protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }
    protected IDbUserRepo<TDbContext, TDbUser, TDbUserId> Users { get; init; }
    protected IDbEntityConverter<TDbUser, User> UserConverter { get; init; }
    protected IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId> Sessions { get; init; }
    protected IDbEntityConverter<TDbSessionInfo, SessionInfo> SessionConverter { get; init; }
    protected ISessionFactory SessionFactory { get; init; }
    protected ITenantResolver<TDbContext> TenantResolver { get; init; }

    public DbAuthService(Options settings, IServiceProvider services) : base(services)
    {
        Settings = settings;
        DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
        Users = services.GetRequiredService<IDbUserRepo<TDbContext, TDbUser, TDbUserId>>();
        UserConverter = services.DbEntityConverter<TDbUser, User>();
        Sessions = services.GetRequiredService<IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();
        SessionConverter = services.DbEntityConverter<TDbSessionInfo, SessionInfo>();
        SessionFactory = services.GetRequiredService<ISessionFactory>();
        TenantResolver = services.GetRequiredService<ITenantResolver<TDbContext>>();
    }

    // Commands

    // [CommandHandler] inherited
    public override async Task SignOut(
        SignOutCommand command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var kickUserSessionHash = command.KickUserSessionHash;
        var kickAllUserSessions = command.KickAllUserSessions;
        var isKickCommand = kickAllUserSessions || !kickUserSessionHash.IsNullOrEmpty();
        var force = command.Force;

        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            if (isKickCommand)
                return;

            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetAuthInfo(session, default);
            if (force) {
                _ = IsSignOutForced(session, default);
                _ = GetOptions(session, default);
            }
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(tenant.Id, invSessionInfo.UserId, default);
                _ = GetUserSessions(tenant.Id, invSessionInfo.UserId, default);
            }
            return;
        }

        // Let's handle special kinds of sign-out first, which only trigger "primary" sign-out version
        if (isKickCommand) {
            var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
            if (user == null)
                return;
            var userSessions = await GetUserSessions(tenant, user.Id, cancellationToken).ConfigureAwait(false);
            var signOutSessions = kickUserSessionHash.IsNullOrEmpty()
                ? userSessions
                : userSessions.Where(p => Equals(p.SessionInfo.SessionHash, kickUserSessionHash));
            foreach (var (sessionId, _) in signOutSessions) {
                var otherSessionSignOutCommand = new SignOutCommand(new Session(sessionId), force);
                await Commander.Run(otherSessionSignOutCommand, isOutermost: true, cancellationToken)
                    .ConfigureAwait(false);
            }
            return;
        }

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo == null! || sessionInfo.IsSignOutForced)
            return;

        context.Operation().Items.Set(sessionInfo);
        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            AuthenticatedIdentity = "",
            UserId = Symbol.Empty,
            IsSignOutForced = force,
        };
        await Sessions.Upsert(dbContext, session.Id, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    // [CommandHandler] inherited
    public override async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null)
                _ = GetUser(tenant.Id, invSessionInfo.UserId, default);
            return;
        }

        var sessionInfo = await GetSessionInfo(session, cancellationToken)
            .Require(SessionInfo.MustBeAuthenticated)
            .ConfigureAwait(false);

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbUserId = DbUserIdHandler.Parse(sessionInfo.UserId, false);
        var dbUser = await Users.Get(dbContext, dbUserId, true, cancellationToken).ConfigureAwait(false);
        if (dbUser == null)
            throw EntityFramework.Internal.Errors.EntityNotFound(Users.UserEntityType);

        await Users.Edit(dbContext, dbUser, command, cancellationToken).ConfigureAwait(false);
        context.Operation().Items.Set(sessionInfo);
    }

    public override async Task UpdatePresence(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo == null)
            return;

        var delta = Clocks.SystemClock.Now - sessionInfo.LastSeenAt;
        if (delta < Settings.MinUpdatePresencePeriod)
            return; // We don't want to update this too frequently

        var command = new SetupSessionCommand(session);
        await Commander.Call(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public override async Task<bool> IsSignOutForced(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo?.IsSignOutForced ?? false;
    }

    // [ComputeMethod] inherited
    public override async Task<SessionAuthInfo?> GetAuthInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        using var _ = Computed.SuspendDependencyCapture();
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo?.ToAuthInfo();
    }

    // [ComputeMethod] inherited
    public override async Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var dbSessionInfo = await Sessions.Get(tenant, session.Id, cancellationToken).ConfigureAwait(false);
        return dbSessionInfo == null ? null : SessionConverter.ToModel(dbSessionInfo);
    }

    // [ComputeMethod] inherited
    public override async Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default)
    {
        using var _ = Computed.SuspendDependencyCapture();
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo?.Options ?? ImmutableOptionSet.Empty;
    }

    // [ComputeMethod] inherited
    public override async Task<User?> GetUser(
        Session session, CancellationToken cancellationToken = default)
    {
        var authInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        if (!(authInfo?.IsAuthenticated() ?? false))
            return null;

        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var user = await GetUser(tenant.Id, authInfo.UserId, cancellationToken).ConfigureAwait(false);
        return user;
    }

    // [ComputeMethod] inherited
    public override async Task<ImmutableArray<SessionInfo>> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return ImmutableArray<SessionInfo>.Empty;

        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var sessions = await GetUserSessions(tenant.Id, user.Id, cancellationToken).ConfigureAwait(false);
        return sessions.Select(p => p.SessionInfo).ToImmutableArray();
    }
}
