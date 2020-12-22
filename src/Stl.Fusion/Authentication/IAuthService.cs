using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion.Authentication
{
    public interface IAuthService
    {
        Task SignOutAsync(bool force, Session session, CancellationToken cancellationToken = default);
        Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session session, CancellationToken cancellationToken = default);
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
        Task SignInAsync(User user, Session session, CancellationToken cancellationToken = default);
    }
}
