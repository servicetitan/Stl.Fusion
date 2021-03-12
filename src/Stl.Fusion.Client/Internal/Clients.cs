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
        Task SignOut([Body] SignOutCommand command, CancellationToken cancellationToken = default);
        [Post("editUser")]
        Task EditUser([Body] EditUserCommand command, CancellationToken cancellationToken = default);
        [Post("updatePresence")]
        Task UpdatePresence([Body] Session session, CancellationToken cancellationToken = default);

        [Get("isSignOutForced"), ComputeMethod]
        Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default);
        [Get("getUser"), ComputeMethod]
        Task<User> GetUser(Session session, CancellationToken cancellationToken = default);
        [Get("getSessionInfo"), ComputeMethod]
        Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default);
        [Get("getUserSessions"), ComputeMethod]
        Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default);
    }
}
