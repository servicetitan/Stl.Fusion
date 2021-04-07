using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Stl.Fusion.Server.Controllers
{
    public class SignInController : Controller
    {
        public class Options
        {
            public string DefaultScheme { get; set; } = "";
        }

        public string DefaultScheme { get; }

        public SignInController(Options? options)
        {
            options ??= new();
            DefaultScheme = options.DefaultScheme;
        }

        [HttpGet("~/signIn")]
        [HttpGet("~/signIn/{scheme}")]
        public virtual IActionResult SignIn(string? scheme = null, string? returnUrl = null)
        {
            scheme ??= DefaultScheme;
            returnUrl ??= "/";
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, scheme);
        }

        [HttpGet("~/signOut")]
        public virtual IActionResult SignOut(string? returnUrl = null)
        {
            // Instruct the cookies middleware to delete the local cookie created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
            returnUrl ??= "/";
            return SignOut(
                new AuthenticationProperties { RedirectUri = returnUrl },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
