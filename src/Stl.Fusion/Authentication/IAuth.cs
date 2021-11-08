using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Authentication;

public interface IAuth
{
    // Commands
    [CommandHandler]
    Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default);
    Task UpdatePresence(Session session, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(KeepAliveTime = 10)]
    Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(KeepAliveTime = 10)]
    Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(KeepAliveTime = 10)]
    Task<User> GetSessionUser(Session session, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default);

    // Non-[ComputeMethod] queries
    Task<Session> GetSession(CancellationToken cancellationToken = default);
}

public interface IAuthBackend
{
    [CommandHandler]
    Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
    [ComputeMethod(KeepAliveTime = 1)]
    Task<User?> GetUser(string userId, CancellationToken cancellationToken = default);
}
