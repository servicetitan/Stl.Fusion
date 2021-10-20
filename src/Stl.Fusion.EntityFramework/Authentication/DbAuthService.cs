using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Authentication.Internal;

namespace Stl.Fusion.EntityFramework.Authentication;

public abstract class DbAuthService<TDbContext> : DbServiceBase<TDbContext>, IServerSideAuthService
    where TDbContext : DbContext
{
    public class Options
    {
        // The default should be less than 3 min - see PresenceService.Options
        public TimeSpan MinUpdatePresencePeriod { get; set; } = TimeSpan.FromMinutes(2.75);
    }

    protected DbAuthService(IServiceProvider services) : base(services) { }

    public abstract Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
    public abstract Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default);
    public abstract Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default);
    public abstract Task UpdatePresence(Session session, CancellationToken cancellationToken = default);
    public abstract Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);

    public abstract Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    public abstract Task<User> GetUser(Session session, CancellationToken cancellationToken = default);
    public abstract Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default);
    public abstract Task<Session> GetSession(CancellationToken cancellationToken = default);
    public abstract Task<User?> TryGetUser(string userId, CancellationToken cancellationToken = default);
}

public class DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> : DbAuthService<TDbContext>
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

    public override async Task SignIn(
        SignInCommand command, CancellationToken cancellationToken = default)
    {
        var (session, user, authenticatedIdentity) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            _ = TryGetUser(invSessionInfo.UserId, default);
            _ = GetUserSessions(invSessionInfo.UserId, default);
            return;
        }

        if (!user.Identities.ContainsKey(authenticatedIdentity))
#pragma warning disable MA0015
            throw new ArgumentOutOfRangeException(
                $"{nameof(command)}.{nameof(SignInCommand.AuthenticatedIdentity)}");
#pragma warning restore MA0015
        if (await IsSignOutForced(session, cancellationToken).ConfigureAwait(false))
            throw Errors.ForcedSignOut();

        await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

        var isNewUser = false;
        var dbUser = await Users
            .TryGetByUserIdentity(dbContext, authenticatedIdentity, cancellationToken)
            .ConfigureAwait(false);
        if (dbUser == null) {
            (dbUser, isNewUser) = await Users
                .GetOrCreateOnSignIn(dbContext, user, cancellationToken)
                .ConfigureAwait(false);
            if (isNewUser == false) {
                UserConverter.UpdateEntity(user, dbUser);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else {
            user = user with {
                Id = DbUserIdHandler.Format(dbUser.Id)
            };
            UserConverter.UpdateEntity(user, dbUser);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo!.IsSignOutForced)
            throw Errors.ForcedSignOut();
        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            AuthenticatedIdentity = authenticatedIdentity,
            UserId = DbUserIdHandler.Format(dbUser.Id)
        };
        context.Operation().Items.Set(sessionInfo);
        context.Operation().Items.Set(isNewUser);
        await Sessions.CreateOrUpdate(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    public override async Task SignOut(
        SignOutCommand command, CancellationToken cancellationToken = default)
    {
        var (session, force) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.TryGet<SessionInfo>();
            if (invSessionInfo != null) {
                _ = TryGetUser(invSessionInfo.UserId, default);
                _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return;
        }

        await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session, cancellationToken).ConfigureAwait(false);
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
        await Sessions.CreateOrUpdate(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    public override async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            _ = TryGetUser(invSessionInfo.UserId, default);
            return;
        }

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (!sessionInfo.IsAuthenticated)
            throw Errors.NotAuthenticated();

        await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

        var dbUserId = DbUserIdHandler.Parse(sessionInfo.UserId);
        var dbUser = await Users.TryGet(dbContext, dbUserId, cancellationToken).ConfigureAwait(false);
        if (dbUser == null)
            throw EntityFramework.Internal.Errors.EntityNotFound(Users.UserEntityType);
        await Users.Edit(dbContext, dbUser, command, cancellationToken).ConfigureAwait(false);
        context.Operation().Items.Set(sessionInfo);
    }

    public override async Task<SessionInfo> SetupSession(
        SetupSessionCommand command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo.IsAuthenticated)
                _ = GetUserSessions(invSessionInfo.UserId, default);
            return null!;
        }

        await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);

        var dbSessionInfo = await Sessions.TryGet(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
        var now = Clocks.SystemClock.Now;
        var oldSessionInfo = SessionConverter.ToModel(dbSessionInfo)
            ?? SessionConverter.NewModel() with { Id = session.Id };
        var newSessionInfo = oldSessionInfo with {
            LastSeenAt = now,
            IPAddress = string.IsNullOrEmpty(ipAddress) ? oldSessionInfo.IPAddress : ipAddress,
            UserAgent = string.IsNullOrEmpty(userAgent) ? oldSessionInfo.UserAgent : userAgent,
        };
        dbSessionInfo = await Sessions.CreateOrUpdate(dbContext, newSessionInfo, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        context.Operation().Items.Set(sessionInfo);
        return sessionInfo!;
    }

    public override async Task UpdatePresence(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        var now = Clocks.SystemClock.Now.ToDateTime();
        var delta = now - sessionInfo.LastSeenAt;
        if (delta < MinUpdatePresencePeriod)
            return; // We don't want to update this too frequently
        var command = new SetupSessionCommand(session).MarkServerSide();
        await SetupSession(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    public override async Task<bool> IsSignOutForced(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo.IsSignOutForced;
    }

    public override async Task<SessionInfo> GetSessionInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var dbSessionInfo = await Sessions.TryGet(session.Id, cancellationToken).ConfigureAwait(false);
        if (dbSessionInfo == null)
            return new(session.Id, Clocks.SystemClock.Now);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        return sessionInfo!;
    }

    public override async Task<User> GetUser(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
            return new User(session.Id);
        var user = await TryGetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        return (user ?? new User(session.Id)).ToClientSideUser();
    }

    public override async Task<User?> TryGetUser(
        string userId, CancellationToken cancellationToken = default)
    {
        var dbUserId = DbUserIdHandler.Parse(userId);
        var dbUser = await Users.TryGet(dbUserId, cancellationToken).ConfigureAwait(false);
        return UserConverter.ToModel(dbUser);
    }

    public override async Task<SessionInfo[]> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return Array.Empty<SessionInfo>();
        return await GetUserSessions(user.Id, cancellationToken).ConfigureAwait(false);
    }

    // Non-[ComputeMethod] queries

    public override Task<Session> GetSession(CancellationToken cancellationToken = default)
        => Task.FromResult(SessionFactory.CreateSession());

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<SessionInfo[]> GetUserSessions(
        string userId, CancellationToken cancellationToken = default)
    {
        if (!DbUserIdHandler.TryParse(userId).IsSome(out var dbUserId))
            return Array.Empty<SessionInfo>();

        await using var dbContext = CreateDbContext();
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var dbSessions = await Sessions.ListByUser(dbContext, dbUserId, cancellationToken).ConfigureAwait(false);
        var sessions = new SessionInfo[dbSessions.Length];
        for (var i = 0; i < dbSessions.Length; i++)
            sessions[i] = SessionConverter.ToModel(dbSessions[i])!;
        return sessions;
    }
}
