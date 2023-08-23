using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Stl.Fusion.Server.Endpoints;

public class BlazorModeEndpoint
{
    public static bool DefaultIsBlazorServer { get; set; } = true;
    public static CookieBuilder Cookie { get; set; } = new() {
        Name = "_ssb_",
        IsEssential = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Expiration = TimeSpan.FromDays(365),
    };

    public virtual Task<RedirectResult> Invoke(HttpContext context, string? isBlazorServer, string? redirectTo = null)
    {
        isBlazorServer = isBlazorServer?.ToLowerInvariant();
        var vIsBlazorServer = isBlazorServer.IsNullOrEmpty()
            ? !IsBlazorServer(context)
            : Equals(isBlazorServer, "1") || Equals(isBlazorServer, "true");
        if (vIsBlazorServer != IsBlazorServer(context)) {
            var response = context.Response;
            response.Cookies.Append(Cookie.Name!, vIsBlazorServer ? "1" : "0", Cookie.Build(context));
        }
        if (redirectTo.IsNullOrEmpty())
            redirectTo = "~/";
        return Task.FromResult(new RedirectResult(redirectTo));
    }

    public static bool IsBlazorServer(HttpContext context)
    {
        var cookies = context.Request.Cookies;
        var cookieValue = cookies.TryGetValue(Cookie.Name!, out var v) ? v : "";
        if (!int.TryParse(cookieValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var isBlazorServerInt32))
            return DefaultIsBlazorServer;
        return isBlazorServerInt32 != 0;
    }

    // Nested types

    public class RedirectResult(string url)
#if NET7_0_OR_GREATER
        : Microsoft.AspNetCore.Http.IResult
#endif
    {
        public string Url { get; init; } = url;

#if NET7_0_OR_GREATER
        public virtual Task ExecuteAsync(HttpContext httpContext)
        {
            var actualResult = Results.Redirect(Url);
            return actualResult.ExecuteAsync(httpContext);
        }
#endif
    }
}
