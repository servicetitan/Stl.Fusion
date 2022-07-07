using System.Globalization;
using Stl.Fusion.Authentication.Commands;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public partial class InMemoryAuthService
{
    // Command handlers

    // [CommandHandler] inherited
    public virtual async Task SignIn(SignInCommand command, CancellationToken cancellationToken = default)
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

        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        sessionInfo ??= new SessionInfo(session, Clocks.SystemClock.Now);
        if (sessionInfo.IsSignOutForced)
            throw Errors.ForcedSignOut();

        var isNewUser = false;
        var userWithAuthenticatedIdentity = GetByUserIdentity(tenant, authenticatedIdentity);
        if (string.IsNullOrEmpty(user.Id)) {
            // No user.Id -> try to find existing user by authenticatedIdentity
            if (userWithAuthenticatedIdentity == null) {
                user = user with { Id = GetNextUserId() };
                isNewUser = true;
            }
            else
                user = MergeUsers(userWithAuthenticatedIdentity, user);
        }
        else {
            // We have Id -> the user exists for sure, but we might need to switch
            // to userWithAuthenticatedIdentity, otherwise we'll register the same
            // UserIdentity for 2 or more users
            _ = long.Parse(user.Id, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var existingUser = Users[(tenant, user.Id)];
            user = userWithAuthenticatedIdentity ?? MergeUsers(existingUser, user);
        }

        // Update user.Version
        user = user with {
            Version = VersionGenerator.NextVersion(user.Version),
        };

        // Update SessionInfo
        sessionInfo = sessionInfo with {
            AuthenticatedIdentity = authenticatedIdentity,
            UserId = user.Id,
        };

        // Persist changes
        Users[(tenant, user.Id)] = user;
        sessionInfo = UpsertSessionInfo(tenant, session.Id, sessionInfo, sessionInfo.Version);
        context.Operation().Items.Set(sessionInfo);
        context.Operation().Items.Set(isNewUser);
    }

    // [CommandHandler] inherited
    public virtual async Task<SessionInfo> SetupSession(
        SetupSessionCommand command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            var invIsNew = context.Operation().Items.GetOrDefault(false);
            if (invIsNew) {
                _ = GetAuthInfo(session, default);
                _ = GetOptions(session, default);
            }
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo?.IsAuthenticated() ?? false)
                _ = GetUserSessions(tenant.Id, invSessionInfo!.UserId, default);
            return null!;
        }

        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        context.Operation().Items.Set(sessionInfo == null); // invIsNew
        sessionInfo ??= new SessionInfo(session, Clocks.SystemClock.Now);
        sessionInfo = sessionInfo with {
            IPAddress = string.IsNullOrEmpty(ipAddress) ? sessionInfo.IPAddress : ipAddress,
            UserAgent = string.IsNullOrEmpty(userAgent) ? sessionInfo.UserAgent : userAgent,
        };
        sessionInfo = UpsertSessionInfo(tenant, session.Id, sessionInfo, sessionInfo.Version);
        context.Operation().Items.Set(sessionInfo); // invSessionInfo
        return sessionInfo;
    }

    // [CommandHandler] inherited
    public virtual async Task SetOptions(SetSessionOptionsCommand command, CancellationToken cancellationToken = default)
    {
        var (session, options, baseVersion) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetOptions(session, default);
            return;
        }

        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        if (sessionInfo == null || sessionInfo.IsSignOutForced)
            throw new KeyNotFoundException();
        sessionInfo = sessionInfo with {
            Options = options
        };
        UpsertSessionInfo(tenant, session.Id, sessionInfo, baseVersion);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public virtual Task<User?> GetUser(Symbol tenantId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.TryGetValue((tenantId, userId), out var user) ? user : null);

    // Protected methods

    protected virtual Task<ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>> GetUserSessions(
        Symbol tenantId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>.Empty);
        var result = SessionInfos
            .Where(kv => kv.Key.TenantId == tenantId && StringComparer.Ordinal.Equals(kv.Value.UserId, userId))
            .OrderByDescending(kv => kv.Value.LastSeenAt)
            .Select(kv => (kv.Key.SessionId, kv.Value))
            .ToImmutableArray();
        return Task.FromResult(result);
    }

    protected virtual SessionInfo UpsertSessionInfo(Symbol tenantId, Symbol sessionId, SessionInfo sessionInfo, long? baseVersion)
    {
        sessionInfo = sessionInfo with {
            Version = VersionGenerator.NextVersion(baseVersion ?? sessionInfo.Version),
            LastSeenAt = Clocks.SystemClock.Now,
        };
#if NETSTANDARD2_0
        var sessionInfo1 = sessionInfo;
        var baseVersion1 = baseVersion;
        SessionInfos.AddOrUpdate((tenantId, sessionId),
            _ => {
                if (baseVersion1.HasValue && baseVersion1.GetValueOrDefault() != 0)
                    throw new VersionMismatchException();
                return sessionInfo1;
            },
            (_, oldSessionInfo) => {
                if (oldSessionInfo.IsSignOutForced)
                    throw Errors.ForcedSignOut();
                if (baseVersion1.HasValue && baseVersion1.GetValueOrDefault() != oldSessionInfo.Version)
                    throw new VersionMismatchException();
                return sessionInfo1.CreatedAt == oldSessionInfo.CreatedAt
                    ? sessionInfo1
                    : sessionInfo1 with {
                        CreatedAt = oldSessionInfo.CreatedAt
                    };
            });
#else
        SessionInfos.AddOrUpdate((tenantId, sessionId),
            static (_, arg) => {
                var (sessionInfo1, baseVersion1) = arg;
                if (baseVersion1.HasValue && baseVersion1.GetValueOrDefault() != 0)
                    throw new VersionMismatchException();
                return sessionInfo1;
            },
            static (_, oldSessionInfo, arg) => {
                var (sessionInfo1, baseVersion1) = arg;
                if (oldSessionInfo.IsSignOutForced)
                    throw Errors.ForcedSignOut();
                if (baseVersion1.HasValue && baseVersion1.GetValueOrDefault() != oldSessionInfo.Version)
                    throw new VersionMismatchException();
                return sessionInfo1.CreatedAt == oldSessionInfo.CreatedAt
                    ? sessionInfo1
                    : sessionInfo1 with {
                        CreatedAt = oldSessionInfo.CreatedAt
                    };
            },
            (sessionInfo, baseVersion));
#endif
        return SessionInfos.GetValueOrDefault((tenantId, sessionId)) ?? sessionInfo;
    }

    protected virtual User? GetByUserIdentity(Symbol tenantId, UserIdentity userIdentity)
        => userIdentity.IsValid
            ? Users.FirstOrDefault(kv => kv.Key.TenantId == tenantId && kv.Value.Identities.ContainsKey(userIdentity)).Value
            : null;

    protected virtual User MergeUsers(User existingUser, User user)
        => existingUser with {
            Claims = existingUser.Claims.SetItems(user.Claims), // Add + replace claims
            Identities = existingUser.Identities.SetItems(user.Identities), // Add + replace identities
        };

    protected string GetNextUserId()
        => Interlocked.Increment(ref _nextUserId).ToString(CultureInfo.InvariantCulture);
}
