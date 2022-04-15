using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.EntityFramework.Authentication;

public partial class DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> : DbAuthService<TDbContext>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUser : DbUser<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected IDbUserIdHandler<TDbUserId> DbUserIdHandler { get; init; }
    protected IDbUserRepo<TDbContext, TDbUser, TDbUserId> Users { get; init; }
    protected IDbEntityConverter<TDbUser, User> UserConverter { get; init; }
    protected IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId> Sessions { get; init; }
    protected IDbEntityConverter<TDbSessionInfo, SessionInfo> SessionConverter { get; init; }
    protected ISessionFactory SessionFactory { get; init; }

    protected TimeSpan MinUpdatePresencePeriod { get; }

    public DbAuthService(Options? options, IServiceProvider services) : base(services)
    {
        options ??= new();
        MinUpdatePresencePeriod = options.MinUpdatePresencePeriod;
        DbUserIdHandler = services.GetRequiredService<IDbUserIdHandler<TDbUserId>>();
        Users = services.GetRequiredService<IDbUserRepo<TDbContext, TDbUser, TDbUserId>>();
        UserConverter = services.DbEntityConverter<TDbUser, User>();
        Sessions = services.GetRequiredService<IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();
        SessionConverter = services.DbEntityConverter<TDbSessionInfo, SessionInfo>();
        SessionFactory = services.GetRequiredService<ISessionFactory>();
    }

    // Commands

    // [CommandHandler] inherited
    public override async Task SignOut(
        SignOutCommand command, CancellationToken cancellationToken = default)
    {
        var (session, force) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetAuthInfo(session, default);
            _ = GetSessionInfo(session, default);
            if (force) {
                _ = IsSignOutForced(session, default);
                _ = GetOptions(session, default);
            }
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(invSessionInfo.UserId, default);
                _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return;
        }

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo!.IsSignOutForced)
            return;

        context.Operation().Items.Set(sessionInfo);
        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            AuthenticatedIdentity = "",
            UserId = "",
            IsSignOutForced = force,
        };
        await Sessions.Upsert(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    // [CommandHandler] inherited
    public override async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null)
                _ = GetUser(invSessionInfo.UserId, default);
            return;
        }

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        sessionInfo = sessionInfo.MustBeAuthenticated();

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbUserId = DbUserIdHandler.Parse(sessionInfo.UserId);
        var dbUser = await Users.Get(dbContext, dbUserId, true, cancellationToken).ConfigureAwait(false);
        if (dbUser == null)
            throw Internal.Errors.EntityNotFound(Users.UserEntityType);
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
        if (delta < MinUpdatePresencePeriod)
            return; // We don't want to update this too frequently
        var command = new SetupSessionCommand(session);
        await SetupSession(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public override async Task<bool> IsSignOutForced(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo.IsSignOutForced;
    }

    // [ComputeMethod] inherited
    public override async Task<SessionAuthInfo> GetAuthInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        using var _ = Computed.SuspendDependencyCapture();
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo == null ? new(session.Id) : sessionInfo.ToAuthInfo();
    }

    // [ComputeMethod] inherited
    public override async Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
    {
        var dbSessionInfo = await Sessions.Get(session.Id, cancellationToken).ConfigureAwait(false);
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
    public override async Task<User> GetUser(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
            return new User(session.Id);
        var user = await GetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        return (user ?? new User(session.Id)).ToClientSideUser();
    }

    // [ComputeMethod] inherited
    public override async Task<SessionInfo[]> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return Array.Empty<SessionInfo>();
        return await GetUserSessions(user.Id, cancellationToken).ConfigureAwait(false);
    }
}
