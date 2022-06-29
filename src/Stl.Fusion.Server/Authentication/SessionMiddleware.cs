using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Stl.Fusion.Authentication;
namespace Stl.Fusion.Server.Authentication;

public class SessionMiddleware : IMiddleware, IHasServices
{
    public record Options
    {
        public CookieBuilder Cookie { get; init; } = new() {
            Name = "FusionAuth.SessionId",
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expiration = TimeSpan.FromDays(28),
        };
        public Func<SessionMiddleware, HttpContext, Task<bool>> ForcedSignOutHandler { get; init; } =
            DefaultForcedSignOutHandler;
        public Func<HttpContext, Symbol> TenantIdExtractor { get; init; } = TenantIdExtractors.None;

        public static async Task<bool> DefaultForcedSignOutHandler(SessionMiddleware self, HttpContext httpContext)
        {
            await httpContext.SignOutAsync().ConfigureAwait(false);
            var url = httpContext.Request.GetEncodedPathAndQuery();
            httpContext.Response.Redirect(url);
            // true:  reload: redirect w/o invoking the next middleware
            // false: proceed normally, i.e. invoke the next middleware
            return true;
        }
    }

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public ILogger Log { get; }

    public IAuth? Auth { get; }
    public ISessionProvider SessionProvider { get; }
    public ISessionFactory SessionFactory { get; }

    public SessionMiddleware(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = services.LogFor(GetType());

        Auth = services.GetService<IAuth>();
        SessionProvider = services.GetRequiredService<ISessionProvider>();
        SessionFactory = services.GetRequiredService<ISessionFactory>();
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        var cancellationToken = httpContext.RequestAborted;
        var cookies = httpContext.Request.Cookies;
        var responseCookies = httpContext.Response.Cookies;
        var cookieName = Settings.Cookie.Name ?? "";
        cookies.TryGetValue(cookieName, out var sessionId);

        var tenantId = Settings.TenantIdExtractor(httpContext);
        var originalSession = string.IsNullOrEmpty(sessionId) ? null : new Session(sessionId);
        var session = originalSession?.WithTenantId(tenantId);

        if (session != null) {
            if (Auth != null) {
                var isSignOutForced = await Auth.IsSignOutForced(session, cancellationToken).ConfigureAwait(false);
                if (isSignOutForced) {
                    await Settings.ForcedSignOutHandler(this, httpContext).ConfigureAwait(false);
                    session = null;
                }
            }
        }
        if (session == null)
            session = SessionFactory.CreateSession().WithTenantId(tenantId);
        if (session != originalSession)
            responseCookies.Append(cookieName, session.Id, Settings.Cookie.Build(httpContext));
        SessionProvider.Session = session;
        await next(httpContext).ConfigureAwait(false);
    }
}
