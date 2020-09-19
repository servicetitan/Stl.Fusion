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
        protected IAuthenticator Authenticator { get; }
        protected ISessionAccessor SessionAccessor { get; }

        public AuthenticatorController(
            IPublisher publisher,
            IAuthenticator authenticator,
            ISessionAccessor sessionAccessor)
            : base(publisher)
        {
            Authenticator = authenticator;
            SessionAccessor = sessionAccessor;
        }

        [HttpGet("logout")]
        public Task LogoutAsync(Session? session = null)
        {
            session ??= SessionAccessor.Session;
            return Authenticator.LogoutAsync(session, HttpContext.RequestAborted);
        }

        [HttpGet("getUser")]
        public Task<Principal> GetUserAsync(Session? session = null)
        {
            session ??= SessionAccessor.Session;
            return PublishAsync(ct => Authenticator.GetUserAsync(session, ct));
        }
    }
}
