using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        [CommandHandler]
        Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken = default);
        Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default);

        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<SessionInfo> GetSessionInfoAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<SessionInfo[]> GetUserSessionsAsync(Session session, CancellationToken cancellationToken = default);
    }

    public interface IServerSideAuthService : IAuthService
    {
        [CommandHandler]
        Task SignInAsync(SignInCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task<SessionInfo> SetupSessionAsync(SetupSessionCommand command, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<User?> TryGetUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
