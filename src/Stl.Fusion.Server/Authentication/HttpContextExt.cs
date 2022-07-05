using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Stl.Fusion.Server.Authentication;

public static class HttpContextExt
{
    public static async Task<AuthenticationScheme[]> GetAuthenticationSchemas(this HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));
        var schemes = httpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var allSchemes = await schemes.GetAllSchemesAsync().ConfigureAwait(false);
        return (
            from scheme in allSchemes
            where !string.IsNullOrEmpty(scheme.DisplayName)
            select scheme
            ).ToArray();
    }

    public static async Task<bool> IsAuthenticationSchemeSupported(this HttpContext httpContext, string scheme)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));
        var authenticationSchemas = await httpContext.GetAuthenticationSchemas().ConfigureAwait(false);
        return (
            from s in authenticationSchemas
            where string.Equals(s.Name, scheme, StringComparison.OrdinalIgnoreCase)
            select s
            ).Any();
    }

    public static IPAddress? GetRemoteIPAddress(this HttpContext context, bool useForwardedForHeaders = true)
    {
        if (useForwardedForHeaders) {
            var headers = context.Request.Headers;
            // If you are allowing CloudFlare headers, you must ensure you are restricting
            // your front-end servers to their IPs: https://www.cloudflare.com/ips/ ,
            // otherwise it can be spoofed.
            var forwardedForHeader = headers["CF-Connecting-IP"].FirstOrDefault()
                ?? headers["X-Forwarded-For"].FirstOrDefault();
            if (IPAddress.TryParse(forwardedForHeader, out var ipAddress))
                return ipAddress;
        }
        return context.Connection.RemoteIpAddress;
    }
}
