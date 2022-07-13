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
        SessionProvider.Session = await GetOrCreateSession(httpContext).ConfigureAwait(false);
        await next(httpContext).ConfigureAwait(false);
    }

    public virtual Session? GetSession(HttpContext httpContext) 
    {
        var cookies = httpContext.Request.Cookies;
        var cookieName = Settings.Cookie.Name ?? "";
        cookies.TryGetValue(cookieName, out var sessionId);
        return string.IsNullOrEmpty(sessionId) ? null : new Session(sessionId);
    }

    public virtual async Task<Session> GetOrCreateSession(HttpContext httpContext) 
    {
        var cancellationToken = httpContext.RequestAborted;
        var originalSession = GetSession(httpContext);
        var tenantId = Settings.TenantIdExtractor(httpContext);
        var session = originalSession?.WithTenantId(tenantId);
        
        if (session != null && Auth != null) {
            var isSignOutForced = await Auth.IsSignOutForced(session, cancellationToken).ConfigureAwait(false);
            if (isSignOutForced) {
                await Settings.ForcedSignOutHandler(this, httpContext).ConfigureAwait(false);
                session = null;
            }
        }
        session ??= SessionFactory.CreateSession().WithTenantId(tenantId);
        
        if (session != originalSession) {
            var cookieName = Settings.Cookie.Name ?? "";
            var responseCookies = httpContext.Response.Cookies;
            responseCookies.Append(cookieName, session.Id, Settings.Cookie.Build(httpContext));
        }
        return session;
    }
}
