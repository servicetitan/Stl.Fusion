using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Client.Authentication
{
    [BasePath("fusion/auth")]
    public interface IAuthClient
    {
        [Get("signOut")]
        Task SignOutAsync(bool force, Session session, CancellationToken cancellationToken = default);
        [Get("saveSessionInfo")]
        Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session session, CancellationToken cancellationToken = default);
        [Get("updatePresence")]
        Task UpdatePresenceAsync(Session session, CancellationToken cancellationToken = default);

        [Get("isSignOutForced"), ComputeMethod]
        Task<bool> IsSignOutForcedAsync(Session session, CancellationToken cancellationToken = default);
        [Get("getUser"), ComputeMethod]
        Task<User> GetUserAsync(Session session, CancellationToken cancellationToken = default);
        [Get("getSessionInfo"), ComputeMethod]
        Task<SessionInfo> GetSessionInfoAsync(Session session, CancellationToken cancellationToken = default);
        [Get("getUserSessions"), ComputeMethod]
        Task<SessionInfo[]> GetUserSessionsAsync(Session session, CancellationToken cancellationToken = default);
    }
}
