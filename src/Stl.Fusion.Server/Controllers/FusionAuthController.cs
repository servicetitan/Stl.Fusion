using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Stl.Fusion.Server.Controllers
{
    public class FusionAuthController : Controller
    {
        public static string DefaultScheme { get; set; } = "";
        public static string CloseWindowRequestPath { get; set; } = "/fusion/close";

        [HttpGet("~/fusion/signin")]
        [HttpGet("~/fusion/signin/{provider}")]
        public virtual IActionResult SignIn(string? scheme = null, string? returnUrl = null)
        {
            scheme ??= DefaultScheme;
            returnUrl ??= "/";
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, scheme);
        }

        [HttpGet("~/fusion/signout")]
        [HttpPost("~/fusion/signout")]
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

        public static bool IsCloseWindowRequest(HttpContext httpContext, out string closeWindowFlowName)
        {
            var request = httpContext.Request;
            var isCloseWindowRequest = request.Path.Value == CloseWindowRequestPath;
            closeWindowFlowName = "";
            if (isCloseWindowRequest && request.Query.TryGetValue("flow", out var flows))
                closeWindowFlowName = flows.FirstOrDefault() ?? "";
            return isCloseWindowRequest;
        }
    }
}
