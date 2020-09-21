using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server.Authentication
{
    [Route("fusion/auth")]
    [ApiController]
    public class AuthenticatorController : FusionController
    {
        protected IAuthService AuthService { get; }
        protected ISessionAccessor SessionAccessor { get; }

        public AuthenticatorController(
            IPublisher publisher,
            IAuthService authService,
            ISessionAccessor sessionAccessor)
            : base(publisher)
        {
            AuthService = authService;
            SessionAccessor = sessionAccessor;
        }

        [HttpGet("logout")]
        public Task LogoutAsync(Session? session = null)
        {
            session ??= SessionAccessor.Session;
            return AuthService.LogoutAsync(session, HttpContext.RequestAborted);
        }

        [HttpGet("getUser")]
        public Task<Principal> GetUserAsync(Session? session = null)
        {
            session ??= SessionAccessor.Session;
            return PublishAsync(ct => AuthService.GetUserAsync(session, ct));
        }
    }
}
