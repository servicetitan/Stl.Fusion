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

        public AuthController(IPublisher publisher, IAuthService authService)
            : base(publisher)
            => AuthService = authService;

        [HttpGet("logout")]
        public Task LogoutAsync(Session? session = null)
        {
            session ??= Session.Current.AssertNotNull();
            return AuthService.LogoutAsync(session, HttpContext.RequestAborted);
        }

        [HttpGet("getUser")]
        public Task<User> GetUserAsync(Session? session = null)
        {
            session ??= Session.Current.AssertNotNull();
            return PublishAsync(ct => AuthService.GetUserAsync(session, ct));
        }
    }
}
