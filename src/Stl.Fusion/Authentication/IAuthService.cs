using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        [CommandHandler]
        Task SignOutAsync(AuthCommand.SignOut command, CancellationToken cancellationToken = default);
        Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default);

        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<SessionInfo> GetSessionInfoAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<SessionInfo[]> GetUserSessionsAsync(Session session, CancellationToken cancellationToken = default);
    }

    public interface IServerSideAuthService : IAuthService
    {
        [CommandHandler]
        Task SignInAsync(AuthCommand.SignIn command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task SaveSessionInfoAsync(AuthCommand.SaveSessionInfo command, CancellationToken cancellationToken = default);
    }
}
