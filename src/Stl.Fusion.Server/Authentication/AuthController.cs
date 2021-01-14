using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server.Authentication
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
        public Task SignOutAsync([FromBody] AuthCommand.SignOut command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return AuthService.SignOutAsync(command, cancellationToken);
        }

        [HttpPost("updatePresence")]
        public Task UpdatePresenceAsync([FromBody] Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.UpdatePresenceAsync(session, cancellationToken);
        }

        // Compute methods

        [HttpGet("isSignOutForced")]
        [Publish]
        public Task<bool> IsSignOutForcedAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.IsSignOutForcedAsync(session, cancellationToken);
        }

        [HttpGet("getUser")]
        [Publish]
        public Task<User> GetUserAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetUserAsync(session, cancellationToken);
        }

        [HttpGet("getSessionInfo")]
        [Publish]
        public Task<SessionInfo> GetSessionInfoAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetSessionInfoAsync(session, cancellationToken);
        }

        [HttpGet("getUserSessions")]
        [Publish]
        public Task<SessionInfo[]> GetUserSessionsAsync(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetUserSessionsAsync(session, cancellationToken);
        }
    }
}
