using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Authentication;

public interface IAuth : IComputeService
{
    // Commands
    [CommandHandler]
    Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default);
    Task UpdatePresence(Session session, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 10)]
    Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<SessionAuthInfo?> GetAuthInfo(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<ImmutableOptionSet> GetOptions(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<User?> GetUser(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<ImmutableArray<SessionInfo>> GetUserSessions(Session session, CancellationToken cancellationToken = default);
}

public interface IAuthBackend : IComputeService
{
    // Commands
    [CommandHandler]
    Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task SetOptions(SetSessionOptionsCommand command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 10)]
    Task<User?> GetUser(Symbol tenantId, string userId, CancellationToken cancellationToken = default);
}
