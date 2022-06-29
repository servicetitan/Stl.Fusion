using Stl.Fusion.Authentication.Commands;
using Stl.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.Authentication.Internal;

public partial class InMemoryAuthService : IAuth, IAuthBackend
{
    private long _nextUserId;
    
    protected ConcurrentDictionary<(Symbol TenantId, Symbol UserId), User> Users { get; } = new();
    protected ConcurrentDictionary<(Symbol TenantId, Symbol SessionId), SessionInfo> SessionInfos { get; } = new();
    protected ISessionFactory SessionFactory { get; }
    protected VersionGenerator<long> VersionGenerator { get; }
    protected ITenantResolver TenantResolver { get; }
    protected ITenantRegistry TenantRegistry { get; }
    protected MomentClockSet Clocks { get; }
    protected ICommander Commander { get; }

    public InMemoryAuthService(IServiceProvider services)
    {
        SessionFactory = services.GetRequiredService<ISessionFactory>();
        VersionGenerator = services.VersionGenerator<long>();
        TenantResolver = services.GetRequiredService<ITenantResolver>();
        TenantRegistry = services.GetRequiredService<ITenantRegistry>();
        Clocks = services.Clocks();
        Commander = services.Commander();
    }

    // Command handlers

    // [CommandHandler] inherited
    public virtual async Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default)
    {
        var (session, force) = command;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(session, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
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
        UpsertSessionInfo(tenant, sessionInfo, null);
    }

    // [CommandHandler] inherited
    public virtual async Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var context = CommandContext.GetCurrent();
        var tenant = await TenantResolver.Resolve(session, context, cancellationToken).ConfigureAwait(false);

        if (Computed.IsInvalidating()) {
            var invSessionInfo = context.Operation().Items.Get<SessionInfo>();
            if (invSessionInfo != null)
                _ = GetUser(tenant.Id, invSessionInfo.UserId, default);
            return;
        }

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        sessionInfo = sessionInfo.MustBeAuthenticated();

        var user = await GetUser(tenant.Id, sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        user = user.MustBeAuthenticated();

        context.Operation().Items.Set(sessionInfo);
        if (command.Name != null) {
            if (command.Name.Length < 3)
                throw new ArgumentOutOfRangeException(nameof(command));
            user = user with {
                Name = command.Name,
                Version = VersionGenerator.NextVersion(user.Version),
            };
        }
        Users[(tenant, user.Id)] = user;
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
        await Commander.Call(command, cancellationToken).ConfigureAwait(false);
    }

    // Compute methods

    // [ComputeMethod] inherited
    public virtual async Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
    {
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        return sessionInfo.IsSignOutForced;
    }

    // [ComputeMethod] inherited
    public virtual async Task<SessionAuthInfo> GetAuthInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        var authInfo = sessionInfo?.ToAuthInfo() ?? new SessionAuthInfo(session.Id);
        if (authInfo.IsSignOutForced) // Let's return a clean SessionAuthInfo in this case
            authInfo = new SessionAuthInfo(authInfo.Id) { IsSignOutForced = true };
        return authInfo;
    }

    // [ComputeMethod] inherited
    public virtual async Task<SessionInfo?> GetSessionInfo(
        Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        return sessionInfo;
    }

    // [ComputeMethod] inherited
    public virtual async Task<ImmutableOptionSet> GetOptions(
        Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var sessionInfo = SessionInfos.GetValueOrDefault((tenant, session.Id));
        return sessionInfo?.Options ?? ImmutableOptionSet.Empty;
    }

    // [ComputeMethod] inherited
    public virtual async Task<User> GetUser(Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var sessionInfo = await GetAuthInfo(session, cancellationToken).ConfigureAwait(false);
        if (sessionInfo.IsSignOutForced || !sessionInfo.IsAuthenticated)
            return new User(session.Id);
        var user = await GetUser(tenant.Id, sessionInfo.UserId, cancellationToken).ConfigureAwait(false);
        return (user ?? new User(session.Id)).ToClientSideUser();
    }

    // [ComputeMethod] inherited
    public virtual async Task<SessionInfo[]> GetUserSessions(
        Session session, CancellationToken cancellationToken = default)
    {
        var tenant = await TenantResolver.Resolve(session, this, cancellationToken).ConfigureAwait(false);
        var user = await GetUser(session, cancellationToken).ConfigureAwait(false);
        if (!user.IsAuthenticated)
            return Array.Empty<SessionInfo>();
        return await GetUserSessions(tenant.Id, user.Id, cancellationToken).ConfigureAwait(false);
    }
}
