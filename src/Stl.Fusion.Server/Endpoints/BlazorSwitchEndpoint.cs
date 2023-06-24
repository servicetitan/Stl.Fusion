using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Stl.Fusion.Server.Endpoints;

public class BlazorSwitchEndpoint
{
    public static bool IsServerSideBlazorDefault { get; set; } = true;
    public static CookieBuilder Cookie { get; set; } = new() {
        Name = "_ssb_",
        IsEssential = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Expiration = TimeSpan.FromDays(365),
    };

    public virtual Task<RedirectResult> Invoke(HttpContext context,
        bool isServerSideBlazor,
        string? redirectTo = null)
    {
        if (isServerSideBlazor != IsServerSideBlazor(context)) {
            var response = context.Response;
            var isServerSideBlazor01 = Convert.ToInt32(isServerSideBlazor).ToString(CultureInfo.InvariantCulture);
            response.Cookies.Append(Cookie.Name!, isServerSideBlazor01, Cookie.Build(context));
        }
        if (redirectTo.IsNullOrEmpty())
            redirectTo = "~/";
        return Task.FromResult(new RedirectResult(redirectTo));
    }

    public static bool IsServerSideBlazor(HttpContext context)
    {
        var cookies = context.Request.Cookies;
        var isSsb = cookies.TryGetValue(Cookie.Name!, out var v) ? v : "";
        if (!int.TryParse(isSsb, NumberStyles.Integer, CultureInfo.InvariantCulture, out var isSsbInt))
            return IsServerSideBlazorDefault;
        return isSsbInt != 0;
    }

    // Nested types

    public class RedirectResult
#if NET7_0_OR_GREATER
        : Microsoft.AspNetCore.Http.IResult
#endif
    {
        public string Url { get; init; }

        public RedirectResult(string url)
            => Url = url;

#if NET7_0_OR_GREATER
        public virtual Task ExecuteAsync(HttpContext httpContext)
        {
            var actualResult = Results.Redirect(Url);
            return actualResult.ExecuteAsync(httpContext);
        }
#endif
    }
}
