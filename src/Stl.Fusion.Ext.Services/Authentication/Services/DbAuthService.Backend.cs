using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Internal;
using Stl.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Services;

public partial class DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>
{
    // Commands

    // [CommandHandler] inherited
    public override async Task SignIn(
        AuthBackend_SignIn command, CancellationToken cancellationToken = default)
    {
        var (session, user, authenticatedIdentity) = (command.Session, command.User, command.AuthenticatedIdentity);
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetAuthInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(tenant.Id, invSessionInfo.UserId, default);
                _ = GetUserSessions(tenant.Id, invSessionInfo.UserId, default);
            }
            return;
        }

        if (!user.Identities.ContainsKey(authenticatedIdentity))
#pragma warning disable MA0015
            throw new ArgumentOutOfRangeException(
                $"{nameof(command)}.{nameof(AuthBackend_SignIn.AuthenticatedIdentity)}");
#pragma warning restore MA0015

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo!.IsSignOutForced)
            throw Errors.SessionUnavailable();

        var isNewUser = false;
        var dbUser = await Users
            .GetByUserIdentity(dbContext, authenticatedIdentity, true, cancellationToken)
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

        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            AuthenticatedIdentity = authenticatedIdentity,
            UserId = DbUserIdHandler.Format(dbUser.Id)
        };
        await Sessions.Upsert(dbContext, session.Id, sessionInfo, cancellationToken).ConfigureAwait(false);

        context.Operation().Items.Set(sessionInfo);
        context.Operation().Items.Set(isNewUser);
    }

    // [CommandHandler] inherited
    public override async Task<SessionInfo> SetupSession(
        AuthBackend_SetupSession command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent, options) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo == null)
                return null!;
            _ = GetSessionInfo(session, default); // Must go first!
            var invIsNew = context.Operation().Items.GetOrDefault<bool>();
            if (invIsNew) {
                _ = GetAuthInfo(session, default);
                _ = GetOptions(session, default);
            }
            if (invSessionInfo.IsAuthenticated())
                _ = GetUserSessions(tenant.Id, invSessionInfo.UserId, default);
            return null!;
        }

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.Get(dbContext, session.Id, false, cancellationToken).ConfigureAwait(false);
        var isNew = dbSessionInfo == null;
        var now = Clocks.SystemClock.Now;
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo)
            ?? SessionConverter.NewModel() with { SessionHash = session.Hash };
        sessionInfo = sessionInfo with {
            LastSeenAt = now,
            IPAddress = ipAddress.IsNullOrEmpty() ? sessionInfo.IPAddress : ipAddress,
            UserAgent = userAgent.IsNullOrEmpty() ? sessionInfo.UserAgent : userAgent,
            Options = options.SetMany(sessionInfo.Options),
        };
        try {
            dbSessionInfo = await Sessions.Upsert(dbContext, session.Id, sessionInfo, cancellationToken)
                .ConfigureAwait(false);
            sessionInfo = SessionConverter.ToModel(dbSessionInfo);
            context.Operation().Items.Set(sessionInfo); // invSessionInfo
            context.Operation().Items.Set(isNew); // invIsNew
            return sessionInfo!;
        }
        catch (DbUpdateException) {
            var scope = context.Items.Get<DbOperationScope<TDbContext>>().Require();
            await scope.Rollback().ConfigureAwait(false);

            var readDbContext = CreateDbContext(tenant);
            await using var __ = readDbContext.ConfigureAwait(false);

            dbSessionInfo = await Sessions.Get(readDbContext, session.Id, false, cancellationToken).ConfigureAwait(false);
            if (dbSessionInfo == null)
                throw; // Something is off: it is supposed to be created concurrently

            sessionInfo = SessionConverter.ToModel(dbSessionInfo);
            return sessionInfo!;
        }
    }

    // [CommandHandler] inherited
    public override async Task SetOptions(
        Auth_SetSessionOptions command, CancellationToken cancellationToken = default)
    {
        var (session, options, expectedVersion) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetOptions(session, default);
            return;
        }

        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);
        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.Get(dbContext, session.Id, false, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo == null)
            throw new KeyNotFoundException();
        VersionChecker.RequireExpected(sessionInfo.Version, expectedVersion);

        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            Options = options,
        };
        await Sessions.Upsert(dbContext, session.Id, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public override async Task<User?> GetUser(
        Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default)
    {
        if (!DbUserIdHandler.TryParse(userId, false, out var dbUserId))
            return null;

        var tenant = TenantRegistry.Get(tenantId);
        var dbUser = await Users.Get(tenant, dbUserId, cancellationToken).ConfigureAwait(false);
        return UserConverter.ToModel(dbUser);
    }

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>> GetUserSessions(
        Symbol tenantId, string userId, CancellationToken cancellationToken = default)
    {
        if (!DbUserIdHandler.TryParse(userId, false, out var dbUserId))
            return ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>.Empty;

        var dbContext = CreateDbContext(tenantId);
        await using var _1 = dbContext.ConfigureAwait(false);
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var _2 = tx.ConfigureAwait(false);

        var dbSessions = await Sessions.ListByUser(dbContext, dbUserId, cancellationToken).ConfigureAwait(false);
        var sessions = dbSessions
            .Select(x => ((Symbol) x.Id, SessionConverter.ToModel(x)!))
            .ToImmutableArray();
        return sessions;
    }
}
