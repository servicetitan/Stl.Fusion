using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client.RestEase;

namespace Stl.Fusion.Client.Authentication
{
    [BasePath("fusion/auth")]
    public interface IAuthClient : IRestEaseReplicaClient
    {
        [Get("logout")]
        Task LogoutAsync(bool force, Session? session = null, CancellationToken cancellationToken = default);
        [Get("saveSessionInfo")]
        Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session? session = null, CancellationToken cancellationToken = default);
        [Get("updatePresence")]
        Task UpdatePresenceAsync(Session? session = null, CancellationToken cancellationToken = default);

        [Get("isLogoutForced"), ComputeMethod]
        Task<bool> IsLogoutForcedAsync(Session? session = null, CancellationToken cancellationToken = default);
        [Get("getUser"), ComputeMethod]
        Task<User> GetUserAsync(Session? session = null, CancellationToken cancellationToken = default);
        [Get("getSessionInfo"), ComputeMethod]
        Task<SessionInfo> GetSessionInfoAsync(Session? session = null, CancellationToken cancellationToken = default);
        [Get("getUserSessions"), ComputeMethod]
        Task<SessionInfo[]> GetUserSessions(Session? session = null, CancellationToken cancellationToken = default);
    }
}
