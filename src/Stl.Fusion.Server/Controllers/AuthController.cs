using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Controllers
{
    [Route("fusion/auth/[action]")]
    [ApiController, JsonifyErrors(RewriteErrors = true)]
    public class AuthController : ControllerBase, IAuthService
    {
        protected IAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }

        public AuthController(IAuthService authService, ISessionResolver sessionResolver)
        {
            AuthService = authService;
            SessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost]
        public Task SignOut([FromBody] SignOutCommand command, CancellationToken cancellationToken = default)
            => AuthService.SignOut(command.UseDefaultSession(SessionResolver), cancellationToken);

        [HttpPost]
        public Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
            => AuthService.EditUser(command.UseDefaultSession(SessionResolver), cancellationToken);

        [HttpPost]
        public Task UpdatePresence([FromBody] Session session, CancellationToken cancellationToken = default)
            => AuthService.UpdatePresence(session, cancellationToken);

        // Queries

        [HttpGet, Publish]
        public Task<bool> IsSignOutForced(Session session, CancellationToken cancellationToken = default)
            => AuthService.IsSignOutForced(session, cancellationToken);

        [HttpGet, Publish]
        public Task<User> GetUser(Session session, CancellationToken cancellationToken = default)
            => AuthService.GetUser(session, cancellationToken);

        [HttpGet, Publish]
        public Task<SessionInfo> GetSessionInfo(Session session, CancellationToken cancellationToken = default)
            => AuthService.GetSessionInfo(session, cancellationToken);

        [HttpGet, Publish]
        public Task<SessionInfo[]> GetUserSessions(Session session, CancellationToken cancellationToken = default)
            => AuthService.GetUserSessions(session, cancellationToken);

        // Non-[ComputeMethod] queries

        [HttpGet, Publish]
        public Task<Session> GetSession(CancellationToken cancellationToken = default)
            => Task.FromResult(SessionResolver.Session);
    }
}
