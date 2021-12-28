using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication.Commands;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public partial class InMemoryAuthService : IAuth, IAuthBackend
{
    private long _nextUserId;
    protected ConcurrentDictionary<Symbol, User> Users { get; } = new();
    protected ConcurrentDictionary<Symbol, SessionInfo> SessionInfos { get; } = new();
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

    // [CommandHandler] inherited
    public virtual async Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default)
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

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo == null || sessionInfo.IsSignOutForced)
            return;

        // Updating SessionInfo
        context.Operation().Items.Set(sessionInfo);
        sessionInfo = sessionInfo with {
            AuthenticatedIdentity = "",
            UserId = "",
            IsSignOutForced = force,
        };
        UpsertSessionInfo(sessionInfo, null);
    }

    // [CommandHandler] inherited
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

    // [CommandHandler] inherited
    public virtual async Task UpdatePresence(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo == null)
            return;
        var delta = Clocks.SystemClock.Now - sessionInfo.LastSeenAt;
        if (delta < TimeSpan.FromSeconds(10))
            return; // We don't want to update this too frequently
        var command = new SetupSessionCommand(session);
        await SetupSession(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public virtual async Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo.IsSignOutForced;
    }

    // [ComputeMethod] inherited
    public virtual Task<SessionAuthInfo> GetAuthInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        var authInfo = sessionInfo?.ToAuthInfo() ?? new SessionAuthInfo(session.Id);
        if (authInfo.IsSignOutForced) // Let's return a clean SessionAuthInfo in this case
            authInfo = new SessionAuthInfo(authInfo.Id) { IsSignOutForced = true };
        return Task.FromResult(authInfo);
    }

    // [ComputeMethod] inherited
    public virtual Task<SessionInfo?> GetSessionInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        return Task.FromResult(sessionInfo)!;
    }

    // [ComputeMethod] inherited
    public virtual Task<ImmutableOptionSet> GetOptions(
        Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = SessionInfos.GetValueOrDefault(session.Id);
        return Task.FromResult(sessionInfo?.Options ?? ImmutableOptionSet.Empty);
    }

    // [ComputeMethod] inherited
    public virtual async Task<User> GetUser(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
            return new User(session.Id);
        var user = await GetUser(sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        return (user ?? new User(session.Id)).ToClientSideUser();
    }

    // [ComputeMethod] inherited
    public virtual async Task<SessionInfo[]> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return Array.Empty<SessionInfo>();
        return await GetUserSessions(user.Id, cancellationToken).ConfigureAwait(false);
    }
}
