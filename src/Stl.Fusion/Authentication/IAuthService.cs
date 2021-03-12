using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        [CommandHandler]
        Task SignOut(SignOutCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default);
        Task UpdatePresence(Session session, CancellationToken cancellationToken = default);

        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<User> GetUser(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default);
    }

    public interface IServerSideAuthService : IAuthService
    {
        [CommandHandler]
        Task SignIn(SignInCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task<SessionInfo> SetupSession(SetupSessionCommand command, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<User?> TryGetUser(string userId, CancellationToken cancellationToken = default);
    }
}
