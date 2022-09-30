using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Stl.Fusion.Server.Controllers;

[Route("~/fusion/blazorMode")]
public class BlazorModeController : ControllerBase
{
    public static bool IsServerSideBlazorDefault { get; set; } = true;
    public static CookieBuilder Cookie { get; set; } = new() {
        Name = "_ssb_",
        IsEssential = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Expiration = TimeSpan.FromDays(365),
    };

    [HttpGet("{isServerSideBlazor}")]
    public IActionResult Switch(bool isServerSideBlazor, string? redirectTo = null)
    {
        if (isServerSideBlazor != IsServerSideBlazor(HttpContext)) {
            var response = HttpContext.Response;
            var isServerSideBlazor01 = Convert.ToInt32(isServerSideBlazor).ToString(CultureInfo.InvariantCulture);
            response.Cookies.Append(Cookie.Name!, isServerSideBlazor01, Cookie.Build(HttpContext));
        }
        if (redirectTo.IsNullOrEmpty())
            redirectTo = "~/";
        return Redirect(redirectTo);
    }

    public static bool IsServerSideBlazor(HttpContext httpContext)
    {
        var cookies = httpContext.Request.Cookies;
        var isSsb = cookies.TryGetValue(Cookie.Name!, out var v) ? v : "";
        if (!int.TryParse(isSsb, NumberStyles.Integer, CultureInfo.InvariantCulture, out var isSsbInt))
            return IsServerSideBlazorDefault;
        return isSsbInt != 0;
    }
}
