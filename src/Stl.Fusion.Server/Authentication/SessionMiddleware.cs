using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Server.Authentication;

public class SessionMiddleware : IMiddleware
{
    public class Options
    {
        public CookieBuilder Cookie { get; set; } = new() {
            Name = "FusionAuth.SessionId",
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expiration = TimeSpan.FromDays(28),
        };
        public Func<SessionMiddleware, HttpContext, Task<bool>> ForcedSignOutHandler { get; set; } =
            DefaultForcedSignOutHandler;

        public static async Task<bool> DefaultForcedSignOutHandler(SessionMiddleware self, HttpContext httpContext)
        {
            await httpContext.SignOutAsync();
            var url = httpContext.Request.GetEncodedPathAndQuery();
            httpContext.Response.Redirect(url);
            // true:  reload: redirect w/o invoking the next middleware
            // false: proceed normally, i.e. invoke the next middleware
            return true;
        }
    }

    public ISessionProvider SessionProvider { get; }
    public ISessionFactory SessionFactory { get; }
    public IAuth? Auth { get; }
    public CookieBuilder Cookie { get; }
    public Func<SessionMiddleware, HttpContext, Task<bool>> ForcedSignOutHandler { get; }

    public SessionMiddleware(
        Options? options,
        ISessionProvider sessionProvider,
        ISessionFactory sessionFactory,
        IAuth? auth = null)
    {
        options ??= new();
        Cookie = options.Cookie;
        ForcedSignOutHandler = options.ForcedSignOutHandler;
        SessionProvider = sessionProvider;
        SessionFactory = sessionFactory;
        Auth = auth;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        var cancellationToken = httpContext.RequestAborted;
        var cookies = httpContext.Request.Cookies;
        var cookieName = Cookie.Name ?? "";
        cookies.TryGetValue(cookieName, out var sessionId);
        var session = string.IsNullOrEmpty(sessionId) ? null : new Session(sessionId);
        if (session != null) {
            if (Auth != null) {
                var isSignOutForced = await Auth.IsSignOutForced(session, cancellationToken);
                if (isSignOutForced) {
                    if (await ForcedSignOutHandler(this, httpContext)) {
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
            responseCookies.Append(cookieName, session.Id, Cookie.Build(httpContext));
        }
        SessionProvider.Session = session;
        await next(httpContext);
    }
}
