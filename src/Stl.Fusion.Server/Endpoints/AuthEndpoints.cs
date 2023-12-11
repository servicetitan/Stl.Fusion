using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Stl.Fusion.Server.Endpoints;

public class AuthEndpoints(AuthEndpoints.Options settings)
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public string DefaultScheme { get; init; } = "";
        public string DefaultSignOutScheme { get; init; } = CookieAuthenticationDefaults.AuthenticationScheme;
        public Action<HttpContext, AuthenticationProperties>? SignInPropertiesBuilder { get; init; } = null;
        public Action<HttpContext, AuthenticationProperties>? SignOutPropertiesBuilder { get; init; } = null;
    }

    public Options Settings { get; } = settings;

    public virtual Task SignIn(
        HttpContext httpContext,
        string? scheme,
        string? returnUrl)
    {
        scheme = scheme.NullIfEmpty() ?? Settings.DefaultScheme;
        returnUrl ??= "/";
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignInPropertiesBuilder?.Invoke(httpContext, properties);
        return httpContext.ChallengeAsync(scheme, properties);
    }

    public virtual Task SignOut(
        HttpContext httpContext,
        string? scheme,
        string? returnUrl)
    {
        // Instruct the cookies middleware to delete the local cookie created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        scheme = scheme.NullIfEmpty() ?? Settings.DefaultSignOutScheme;
        returnUrl ??= "/";
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignOutPropertiesBuilder?.Invoke(httpContext, properties);
        return httpContext.SignOutAsync(scheme, properties);
    }
}
