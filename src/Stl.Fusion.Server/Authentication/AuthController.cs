using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server.Authentication
{
    [Route("fusion/auth")]
    [ApiController]
    public class AuthController : FusionController
    {
        protected IAuthService AuthService { get; }
        protected IAuthSessionAccessor AuthSessionAccessor { get; }

        public AuthController(
            IPublisher publisher,
            IAuthService authService,
            IAuthSessionAccessor authSessionAccessor)
            : base(publisher)
        {
            AuthService = authService;
            AuthSessionAccessor = authSessionAccessor;
        }

        [HttpGet("logout")]
        public Task LogoutAsync(AuthSession? session = null)
        {
            session ??= AuthSessionAccessor.Session;
            return AuthService.LogoutAsync(session, HttpContext.RequestAborted);
        }

        [HttpGet("getUser")]
        public Task<AuthUser> GetUserAsync(AuthSession? session = null)
        {
            session ??= AuthSessionAccessor.Session;
            return PublishAsync(ct => AuthService.GetUserAsync(session, ct));
        }
    }
}
