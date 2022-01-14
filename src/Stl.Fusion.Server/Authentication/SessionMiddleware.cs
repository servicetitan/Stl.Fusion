using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Stl.Fusion.Authentication;
namespace Stl.Fusion.Server.Authentication;

public class SessionMiddleware : IMiddleware
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

    public static Options DefaultSettings { get; set; } = new();

    public Options Settings { get; }
    public ISessionProvider SessionProvider { get; }
    public ISessionFactory SessionFactory { get; }
    public IAuth? Auth { get; }

    public SessionMiddleware(
        Options? settings,
        ISessionProvider sessionProvider,
        ISessionFactory sessionFactory,
        IAuth? auth = null)
    {
        Settings = settings ?? DefaultSettings;
        SessionProvider = sessionProvider;
        SessionFactory = sessionFactory;
        Auth = auth;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        var cancellationToken = httpContext.RequestAborted;
        var cookies = httpContext.Request.Cookies;
        var cookieName = Settings.Cookie.Name ?? "";
        cookies.TryGetValue(cookieName, out var sessionId);
        var session = string.IsNullOrEmpty(sessionId) ? null : new Session(sessionId);
        if (session != null) {
            if (Auth != null) {
                var isSignOutForced = await Auth.IsSignOutForced(session, cancellationToken).ConfigureAwait(false);
                if (isSignOutForced) {
                    if (await Settings.ForcedSignOutHandler(this, httpContext).ConfigureAwait(false)) {
                        var responseCookies = httpContext.Response.Cookies;
                        responseCookies.Delete(cookieName);
                        return;
                    }
                    session = null;
                }
            }
        }
        if (session == null) {
            session = SessionFactory.CreateSession();
            var responseCookies = httpContext.Response.Cookies;
            responseCookies.Append(cookieName, session.Id, Settings.Cookie.Build(httpContext));
        }
        SessionProvider.Session = session;
        await next(httpContext).ConfigureAwait(false);
    }
}
