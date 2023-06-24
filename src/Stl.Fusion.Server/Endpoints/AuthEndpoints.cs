using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Stl.Fusion.Server.Endpoints;

public class AuthEndpoints
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

    public AuthEndpoints(Options settings)
        => Settings = settings;

    public virtual Task<ChallengeResult> SignIn(HttpContext context,
        string? scheme,
        string? returnUrl = null)
    {
        scheme ??= Settings.DefaultScheme;
        returnUrl ??= "/";
        var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignInPropertiesBuilder?.Invoke(context, authenticationProperties);
        return Task.FromResult(new ChallengeResult(authenticationProperties, scheme));
    }

    public virtual Task<SignOutResult> SignOut(HttpContext context,
        string? returnUrl = null)
    {
        // Instruct the cookies middleware to delete the local cookie created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        returnUrl ??= "/";
        var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
        Settings.SignOutPropertiesBuilder?.Invoke(context, authenticationProperties);
        return Task.FromResult(new SignOutResult(authenticationProperties, Settings.SignOutAuthenticationScheme));
    }

    // Nested types

    public class ChallengeResult
#if NET7_0_OR_GREATER
        : Microsoft.AspNetCore.Http.IResult
#endif
    {
        public AuthenticationProperties AuthenticationProperties { get; init; }
        public string[] AuthenticationSchemes { get; init; }

        public ChallengeResult(AuthenticationProperties authenticationProperties, params string[] authenticationSchemes)
        {
            AuthenticationProperties = authenticationProperties;
            AuthenticationSchemes = authenticationSchemes;
        }

#if NET7_0_OR_GREATER
        public virtual Task ExecuteAsync(HttpContext httpContext)
        {
            var actualResult = Results.Challenge(AuthenticationProperties, AuthenticationSchemes);
            return actualResult.ExecuteAsync(httpContext);
        }
#endif
    }

    public class SignOutResult
#if NET7_0_OR_GREATER
        : Microsoft.AspNetCore.Http.IResult
#endif
    {
        public AuthenticationProperties AuthenticationProperties { get; init; }
        public string[] AuthenticationSchemes { get; init; }

        public SignOutResult(AuthenticationProperties authenticationProperties, params string[] authenticationSchemes)
        {
            AuthenticationProperties = authenticationProperties;
            AuthenticationSchemes = authenticationSchemes;
        }

#if NET7_0_OR_GREATER
        public virtual Task ExecuteAsync(HttpContext httpContext)
        {
            var actualResult = Results.SignOut(AuthenticationProperties, AuthenticationSchemes);
            return actualResult.ExecuteAsync(httpContext);
        }
#endif
    }
}
