using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server.Authentication
{
    [Route("fusion/auth")]
    [ApiController]
    public class AuthController : FusionController, IAuthService
    {
        protected IAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }

        public AuthController(IAuthService authService, ISessionResolver sessionResolver)
        {
            AuthService = authService;
            SessionResolver = sessionResolver;
        }

        [HttpGet("signOut")]
        public Task SignOutAsync(bool force, Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.SignOutAsync(force, session, cancellationToken);
        }

        [HttpGet("saveSessionInfo")]
        public Task SaveSessionInfoAsync(SessionInfo sessionInfo, Session? session = null,
            CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.SaveSessionInfoAsync(sessionInfo, session, cancellationToken);
        }

        [HttpGet("updatePresence")]
        public Task UpdatePresenceAsync(Session? session = null, CancellationToken cancellationToken = default)
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
        public Task<SessionInfo[]> GetUserSessions(Session? session = null, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return AuthService.GetUserSessions(session, cancellationToken);
        }
    }
}
