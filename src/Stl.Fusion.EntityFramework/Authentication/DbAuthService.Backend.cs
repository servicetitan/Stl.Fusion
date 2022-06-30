using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Authentication.Internal;
using Stl.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework.Authentication;

public partial class DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId> : DbAuthService<TDbContext>
{
    // Commands

    // [CommandHandler] inherited
    public override async Task SignIn(
        SignInCommand command, CancellationToken cancellationToken = default)
    {
        var (session, user, authenticatedIdentity) = command;
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
                $"{nameof(command)}.{nameof(SignInCommand.AuthenticatedIdentity)}");
#pragma warning restore MA0015
        if (await IsSignOutForced(session, cancellationToken).ConfigureAwait(false))
            throw Errors.ForcedSignOut();

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var isNewUser = false;
        var dbUser = await Users
            .GetByUserIdentity(dbContext, authenticatedIdentity, cancellationToken)
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

        var dbSessionInfo = await Sessions.GetOrCreate(dbContext, session.Id, cancellationToken).ConfigureAwait(false);
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
        await Sessions.Upsert(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    // [CommandHandler] inherited
    public override async Task<SessionInfo> SetupSession(
        SetupSessionCommand command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo == null)
                return null!;
            _ = GetSessionInfo(session, default); // Must go first!
            var invIsNew = context.Operation().Items.GetOrDefault(false);
            if (invIsNew) {
                _ = GetAuthInfo(session, default);
                _ = GetOptions(session, default);
            }
            if (invSessionInfo.IsAuthenticated)
                _ = GetUserSessions(tenant.Id, invSessionInfo.UserId, default);
            return null!;
        }

        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.Get(dbContext, session.Id, true, cancellationToken).ConfigureAwait(false);
        var isNew = dbSessionInfo == null;
        var now = Clocks.SystemClock.Now;
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo)
            ?? SessionConverter.NewModel() with { Id = session.Id };
        sessionInfo = sessionInfo with {
            LastSeenAt = now,
            IPAddress = string.IsNullOrEmpty(ipAddress) ? sessionInfo.IPAddress : ipAddress,
            UserAgent = string.IsNullOrEmpty(userAgent) ? sessionInfo.UserAgent : userAgent,
        };
        try {
            dbSessionInfo = await Sessions.Upsert(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
            sessionInfo = SessionConverter.ToModel(dbSessionInfo);
            context.Operation().Items.Set(sessionInfo); // invSessionInfo
            context.Operation().Items.Set(isNew); // invIsNew
            return sessionInfo!;
        }
        catch (DbUpdateException) {
            var scope = context.Items.Get<DbOperationScope<TDbContext>>();
            await scope!.Rollback().ConfigureAwait(false);

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
        SetSessionOptionsCommand command, CancellationToken cancellationToken = default)
    {
        var (session, options, baseVersion) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetOptions(session, default);
            return;
        }

        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);
        var dbContext = await CreateCommandDbContext(tenant, cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var dbSessionInfo = await Sessions.Get(dbContext, session.Id, true, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionConverter.ToModel(dbSessionInfo);
        if (sessionInfo == null)
            throw new KeyNotFoundException();
        if (baseVersion.HasValue && sessionInfo.Version != baseVersion.GetValueOrDefault())
            throw new VersionMismatchException();

        sessionInfo = sessionInfo with {
            LastSeenAt = Clocks.SystemClock.Now,
            Options = options,
        };
        await Sessions.Upsert(dbContext, sessionInfo, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public override async Task<User?> GetUser(
        Symbol tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var dbUserId = DbUserIdHandler.Parse(userId);
        var tenant = TenantRegistry.Get(tenantId);
        var dbUser = await Users.Get(tenant, dbUserId, cancellationToken).ConfigureAwait(false);
        return UserConverter.ToModel(dbUser);
    }

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<SessionInfo[]> GetUserSessions(
        Symbol tenantId, string userId, CancellationToken cancellationToken = default)
    {
        if (!DbUserIdHandler.TryParse(userId).IsSome(out var dbUserId))
            return Array.Empty<SessionInfo>();

        var dbContext = CreateDbContext(tenantId);
        await using var _1 = dbContext.ConfigureAwait(false);
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var _2 = tx.ConfigureAwait(false);

        var dbSessions = await Sessions.ListByUser(dbContext, dbUserId, cancellationToken).ConfigureAwait(false);
        var sessions = new SessionInfo[dbSessions.Length];
        for (var i = 0; i < dbSessions.Length; i++)
            sessions[i] = SessionConverter.ToModel(dbSessions[i])!;
        return sessions;
    }
}
