using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Stl.Fusion.Server.Controllers;

public class SignInController : Controller
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string DefaultScheme { get; init; } = "";
        public string SignOutAuthenticationScheme { get; init; } = CookieAuthenticationDefaults.AuthenticationScheme;
        public Action<HttpContext, AuthenticationProperties>? SignInPropertiesBuilder { get; init; } = null;
        public Action<HttpContext, AuthenticationProperties>? SignOutPropertiesBuilder { get; init; } = null;
    }

    public Options Settings { get; }

    public SignInController(Options settings)
        => Settings = settings;

    [HttpGet("~/signIn")]
    [HttpGet("~/signIn/{scheme}")]
    public virtual IActionResult SignIn(string? scheme = null, string? returnUrl = null)
    {
        scheme ??= Settings.DefaultScheme;
        returnUrl ??= "/";
        var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignInPropertiesBuilder?.Invoke(HttpContext, authenticationProperties);
        return Challenge(authenticationProperties, scheme);
    }

    [HttpGet("~/signOut")]
    public virtual IActionResult SignOut(string? returnUrl = null)
    {
        // Instruct the cookies middleware to delete the local cookie created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        returnUrl ??= "/";
        var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignOutPropertiesBuilder?.Invoke(HttpContext, authenticationProperties);
        return SignOut(authenticationProperties, Settings.SignOutAuthenticationScheme);
    }
}
