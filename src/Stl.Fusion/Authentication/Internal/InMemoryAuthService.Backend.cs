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

        // First, let's validate user.Id
        if (!user.Id.IsEmpty)
            _ = long.Parse(user.Id, NumberStyles.Integer, CultureInfo.InvariantCulture);

        // And find the existing user
        var existingUser = GetByUserIdentity(tenant, authenticatedIdentity);
        if (existingUser == null && !user.Id.IsEmpty)
            existingUser = Users.GetValueOrDefault((tenant, user.Id));

        if (existingUser != null) {
            // Merge if found
            user = MergeUsers(existingUser, user);
        }
        else {
            // Otherwise, create a new one
            if (user.Id.IsEmpty)
                user = user with { Id = GetNextUserId() };
            isNewUser = true;
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
        var (session, ipAddress, userAgent, options) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(command, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            var invIsNew = context.Operation().Items.GetOrDefault<bool>();
            if (invIsNew) {
                _ = GetAuthInfo(session, default);
                _ = GetOptions(session, default);
            }
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo?.IsAuthenticated() ?? false)
                _ = GetUserSessions(tenant.Id, invSessionInfo.UserId, default);
            return null!;
        }

        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        context.Operation().Items.Set(sessionInfo == null); // invIsNew
        sessionInfo ??= new SessionInfo(session, Clocks.SystemClock.Now);
        sessionInfo = sessionInfo with {
            IPAddress = ipAddress.IsNullOrEmpty() ? sessionInfo.IPAddress : ipAddress,
            UserAgent = userAgent.IsNullOrEmpty() ? sessionInfo.UserAgent : userAgent,
            Options = options.SetMany(sessionInfo.Options),
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
    public virtual Task<User?> GetUser(Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.TryGetValue((tenantId, userId), out var user) ? user : null);

    // Protected methods

    protected virtual Task<ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>> GetUserSessions(
        Symbol tenantId, Symbol userId, CancellationToken cancellationToken = default)
    {
        if (userId.IsEmpty)
            return Task.FromResult(ImmutableArray<(Symbol Id, SessionInfo SessionInfo)>.Empty);
        var result = SessionInfos
            .Where(kv => kv.Key.TenantId == tenantId && kv.Value.UserId == userId)
            .OrderByDescending(kv => kv.Value.LastSeenAt)
            .Select(kv => (kv.Key.SessionId, kv.Value))
            .ToImmutableArray();
        return Task.FromResult(result);
    }

    protected virtual SessionInfo UpsertSessionInfo(Symbol tenantId, Symbol sessionId, SessionInfo sessionInfo, long? expectedVersion)
    {
        sessionInfo = sessionInfo with {
            Version = VersionGenerator.NextVersion(expectedVersion ?? sessionInfo.Version),
            LastSeenAt = Clocks.SystemClock.Now,
        };
#if NETSTANDARD2_0
        var sessionInfo1 = sessionInfo;
        var expectedVersion1 = expectedVersion;
        SessionInfos.AddOrUpdate((tenantId, sessionId),
            _ => {
                VersionChecker.RequireExpected(0L, expectedVersion1);
                return sessionInfo1;
            },
            (_, oldSessionInfo) => {
                if (oldSessionInfo.IsSignOutForced)
                    throw Errors.ForcedSignOut();
                VersionChecker.RequireExpected(oldSessionInfo.Version, expectedVersion1);
                return sessionInfo1.CreatedAt == oldSessionInfo.CreatedAt
                    ? sessionInfo1
                    : sessionInfo1 with {
                        CreatedAt = oldSessionInfo.CreatedAt
                    };
            });
#else
        SessionInfos.AddOrUpdate((tenantId, sessionId),
            static (_, arg) => {
                var (sessionInfo1, expectedVersion1) = arg;
                VersionChecker.RequireExpected(0L, expectedVersion1);
                return sessionInfo1;
            },
            static (_, oldSessionInfo, arg) => {
                var (sessionInfo1, expectedVersion1) = arg;
                if (oldSessionInfo.IsSignOutForced)
                    throw Errors.ForcedSignOut();
                VersionChecker.RequireExpected(oldSessionInfo.Version, expectedVersion1);
                return sessionInfo1.CreatedAt == oldSessionInfo.CreatedAt
                    ? sessionInfo1
                    : sessionInfo1 with {
                        CreatedAt = oldSessionInfo.CreatedAt
                    };
            },
            (sessionInfo, baseVersion: expectedVersion));
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
