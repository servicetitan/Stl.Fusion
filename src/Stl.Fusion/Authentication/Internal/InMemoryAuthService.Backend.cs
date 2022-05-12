using System.Globalization;
using Stl.Fusion.Authentication.Commands;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public partial class InMemoryAuthService : IAuth, IAuthBackend
{
    // Command handlers

    // [CommandHandler] inherited
    public virtual Task SignIn(SignInCommand command, CancellationToken cancellationToken = default)
    {
        var (session, user, authenticatedIdentity) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetAuthInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(invSessionInfo.UserId, default);
                _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return Task.CompletedTask;
        }

        if (!user.Identities.ContainsKey(authenticatedIdentity))
            throw new ArgumentOutOfRangeException(
                $"{nameof(command)}.{nameof(SignInCommand.AuthenticatedIdentity)}");

        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        sessionInfo ??= new SessionInfo(session.Id, Clocks.SystemClock.Now);
        if (sessionInfo.IsSignOutForced)
            throw Errors.ForcedSignOut();

        var isNewUser = false;
        var userWithAuthenticatedIdentity = GetByUserIdentity(authenticatedIdentity);
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
            var existingUser = Users[user.Id];
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
        Users[user.Id] = user;
        sessionInfo = UpsertSessionInfo(sessionInfo, sessionInfo.Version);
        context.Operation().Items.Set(sessionInfo);
        context.Operation().Items.Set(isNewUser);
        return Task.CompletedTask;
    }

    // [CommandHandler] inherited
    public virtual Task<SessionInfo> SetupSession(
        SetupSessionCommand command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            var invIsNew = context.Operation().Items.GetOrDefault(false);
            if (invIsNew) {
                _ = GetAuthInfo(session, default);
                _ = GetOptions(session, default);
            }
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo is { IsAuthenticated: true })
                _ = GetUserSessions(invSessionInfo.UserId, default);
            return Task.FromResult<SessionInfo>(null!);
        }

        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        context.Operation().Items.Set(sessionInfo == null); // invIsNew
        sessionInfo ??= new SessionInfo(session.Id, Clocks.SystemClock.Now);
        sessionInfo = sessionInfo with {
            IPAddress = string.IsNullOrEmpty(ipAddress) ? sessionInfo.IPAddress : ipAddress,
            UserAgent = string.IsNullOrEmpty(userAgent) ? sessionInfo.UserAgent : userAgent,
        };
        sessionInfo = UpsertSessionInfo(sessionInfo, sessionInfo.Version);
        context.Operation().Items.Set(sessionInfo); // invSessionInfo
        return Task.FromResult(sessionInfo);
    }

    // [CommandHandler] inherited
    public virtual Task SetOptions(SetSessionOptionsCommand command, CancellationToken cancellationToken = default)
    {
        var (session, options, baseVersion) = command;
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default); // Must go first!
            _ = GetOptions(session, default);
            return Task.CompletedTask;
        }

        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        if (sessionInfo == null || sessionInfo.IsSignOutForced)
            throw new KeyNotFoundException();
        sessionInfo = sessionInfo with {
            Options = options
        };
        UpsertSessionInfo(sessionInfo, baseVersion);
        return Task.CompletedTask;
    }

    // Compute methods

    // [ComputeMethod] inherited
    public virtual Task<User?> GetUser(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.TryGetValue(userId, out var user) ? user : null);

    // Protected methods

    protected virtual Task<SessionInfo[]> GetUserSessions(
        string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(Array.Empty<SessionInfo>());
        var result = SessionInfos.Values
            .Where(si => StringComparer.Ordinal.Equals(si.UserId, userId))
            .OrderByDescending(si => si.LastSeenAt)
            .ToArray();
        return Task.FromResult(result);
    }

    protected virtual SessionInfo UpsertSessionInfo(SessionInfo sessionInfo, long? baseVersion)
    {
        sessionInfo = sessionInfo with {
            Version = VersionGenerator.NextVersion(baseVersion ?? sessionInfo.Version),
            LastSeenAt = Clocks.SystemClock.Now,
        };
#if NETSTANDARD2_0
        var sessionInfo1 = sessionInfo;
        var baseVersion1 = baseVersion;
        SessionInfos.AddOrUpdate(sessionInfo.Id,
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
        SessionInfos.AddOrUpdate(sessionInfo.Id,
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
        return SessionInfos.GetValueOrDefault(sessionInfo.Id) ?? sessionInfo;
    }

    protected virtual User? GetByUserIdentity(UserIdentity userIdentity)
        => userIdentity.IsValid
            ? Users.Values.FirstOrDefault(user => user.Identities.ContainsKey(userIdentity))
            : null;

    protected virtual User MergeUsers(User existingUser, User user)
        => existingUser with {
            Claims = existingUser.Claims.SetItems(user.Claims), // Add + replace claims
            Identities = existingUser.Identities.SetItems(user.Identities), // Add + replace identities
        };

    protected string GetNextUserId()
        => Interlocked.Increment(ref _nextUserId).ToString(CultureInfo.InvariantCulture);
}
