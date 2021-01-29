using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Client.Internal
{
    [BasePath("fusion/auth")]
    public interface IAuthClient
    {
        [Post("signOut")]
        Task SignOutAsync([Body] SignOutCommand command, CancellationToken cancellationToken = default);
        [Post("updatePresence")]
        Task UpdatePresenceAsync([Body] Session session, CancellationToken cancellationToken = default);

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
