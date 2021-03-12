using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Controllers
{
    [Route("fusion/auth")]
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

        [HttpPost("signOut")]
        public Task SignOut([FromBody] SignOutCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return AuthService.SignOut(command, cancellationToken);
        }

        [HttpPost("editUser")]
        public Task EditUser(EditUserCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return AuthService.EditUser(command, cancellationToken);
        }

        [HttpPost("updatePresence")]
        public Task UpdatePresence([FromBody] Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.UpdatePresence(session, cancellationToken);
        }

        // Compute methods

        [HttpGet("isSignOutForced")]
        [Publish]
        public Task<bool> IsSignOutForced(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.IsSignOutForced(session, cancellationToken);
        }

        [HttpGet("getUser")]
        [Publish]
        public Task<User> GetUser(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetUser(session, cancellationToken);
        }

        [HttpGet("getSessionInfo")]
        [Publish]
        public Task<SessionInfo> GetSessionInfo(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetSessionInfo(session, cancellationToken);
        }

        [HttpGet("getUserSessions")]
        [Publish]
        public Task<SessionInfo[]> GetUserSessions(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetUserSessions(session, cancellationToken);
        }
    }
}
