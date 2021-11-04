using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication.Commands;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public class InMemoryAuthService : IServerSideAuthService
{
    private long _nextUserId;
    protected ConcurrentDictionary<string, User> Users { get; } = new();
    protected ConcurrentDictionary<string, SessionInfo> SessionInfos { get; } = new();
    protected ISessionFactory SessionFactory { get; }
    protected MomentClockSet Clocks { get; }
    protected VersionGenerator<long> VersionGenerator { get; }

    public InMemoryAuthService(IServiceProvider services)
    {
        SessionFactory = services.GetRequiredService<ISessionFactory>();
        Clocks = services.Clocks();
        VersionGenerator = services.VersionGenerator<long>();
    }

    // Command handlers

    public virtual async Task SignIn(SignInCommand command, CancellationToken cancellationToken = default)
    {
        var (session, user, authenticatedIdentity) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(invSessionInfo.UserId, default);
                _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return;
        }

        if (!user.Identities.ContainsKey(authenticatedIdentity))
            throw new ArgumentOutOfRangeException(
                $"{nameof(command)}.{nameof(SignInCommand.AuthenticatedIdentity)}");
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
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
            _ = long.Parse(user.Id); // Ensure exception is the same as for DbAuthService
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
        sessionInfo = AddOrUpdateSessionInfo(sessionInfo);
        context.Operation().Items.Set(sessionInfo);
        context.Operation().Items.Set(isNewUser);
    }

    public virtual async Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default)
    {
        var (session, force) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            if (force)
                _ = IsSignOutForced(session, default);
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                _ = GetUser(invSessionInfo.UserId, default);
                _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return;
        }

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced)
            return;

        // Updating SessionInfo
        context.Operation().Items.Set(sessionInfo);
        sessionInfo = sessionInfo with {
            AuthenticatedIdentity = "",
            UserId = "",
            IsSignOutForced = force,
        };
        AddOrUpdateSessionInfo(sessionInfo);
    }

    public virtual async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
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
        var user = await GetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        user = user.MustBeAuthenticated();

        context.Operation().Items.Set(sessionInfo);
        if (command.Name != null)
            user = user with {
                Name = command.Name,
                Version = VersionGenerator.NextVersion(user.Version),
            };
        Users[user.Id] = user;
    }

    public virtual async Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default)
    {
        var (session, ipAddress, userAgent) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = GetSessionInfo(session, default);
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null) {
                if (invSessionInfo.IsAuthenticated)
                    _ = GetUserSessions(invSessionInfo.UserId, default);
            }
            return null!;
        }
        var oldSessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        var newSessionInfo = oldSessionInfo with {
            IPAddress = string.IsNullOrEmpty(ipAddress) ? oldSessionInfo.IPAddress : ipAddress,
            UserAgent = string.IsNullOrEmpty(userAgent) ? oldSessionInfo.UserAgent : userAgent,
        };
        newSessionInfo = AddOrUpdateSessionInfo(newSessionInfo);
        context.Operation().Items.Set(newSessionInfo);
        return newSessionInfo;
    }

    public virtual async Task UpdatePresence(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        var now = Clocks.SystemClock.Now.ToDateTime();
        var delta = now - sessionInfo.LastSeenAt;
        if (delta < TimeSpan.FromSeconds(10))
            return; // We don't want to update this too frequently
        var command = new SetupSessionCommand(session).MarkServerSide();
        await SetupSession(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    public virtual async Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo.IsSignOutForced;
    }

    public virtual Task<SessionInfo> GetSessionInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = SessionInfos.GetValueOrDefault(session);
        sessionInfo = sessionInfo.OrDefault(session, Clocks); // To mask signed out sessions
        return Task.FromResult(sessionInfo);
    }

    public virtual async Task<User> GetUser(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
            return new User(session.Id);
        var user = await GetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        return (user ?? new User(session.Id)).ToClientSideUser();
    }

    public virtual Task<User?> GetUser(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.TryGetValue(userId, out var user) ? user : null);

    public virtual async Task<SessionInfo[]> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return Array.Empty<SessionInfo>();
        return await GetUserSessions(user.Id, cancellationToken).ConfigureAwait(false);
    }

    // Non-[ComputeMethod] queries

    public virtual Task<Session> GetSession(CancellationToken cancellationToken = default)
        => Task.FromResult(SessionFactory.CreateSession());

    // Protected methods

    [ComputeMethod]
    protected virtual Task<SessionInfo[]> GetUserSessions(
        string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(Array.Empty<SessionInfo>());
        var result = SessionInfos.Values
            .Where(si => si.UserId == userId)
            .OrderByDescending(si => si.LastSeenAt)
            .ToArray();
        return Task.FromResult(result);
    }

    protected virtual SessionInfo AddOrUpdateSessionInfo(SessionInfo sessionInfo)
    {
        sessionInfo = sessionInfo with {
            Version = VersionGenerator.NextVersion(sessionInfo.Version),
            LastSeenAt = Clocks.SystemClock.Now,
        };
        SessionInfos.AddOrUpdate(sessionInfo.Id, sessionInfo, (sessionId, oldSessionInfo) => {
            if (oldSessionInfo.IsSignOutForced)
                throw Errors.ForcedSignOut();
            return sessionInfo.CreatedAt == oldSessionInfo.CreatedAt
                ? sessionInfo
                : sessionInfo with {
                    CreatedAt = oldSessionInfo.CreatedAt
                };
        });
        return SessionInfos.GetValueOrDefault(sessionInfo.Id) ?? sessionInfo;
    }

    protected virtual User? GetByUserIdentity(UserIdentity userIdentity)
        => userIdentity.IsValid
            ? Users.Values.FirstOrDefault(user => user.Identities.ContainsKey(userIdentity))
            : null;

    protected virtual User MergeUsers(User existingUser, User user)
        => existingUser with {
            Claims = user.Claims.SetItems(existingUser.Claims), // Add new claims
            Identities = existingUser.Identities.SetItems(user.Identities), // Add + replace identities
        };

    protected string GetNextUserId()
        => Interlocked.Increment(ref _nextUserId).ToString();
}
