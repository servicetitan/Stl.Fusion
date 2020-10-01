using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Generators;

namespace Stl.Fusion.Server.Authentication
{
    public class SessionMiddleware : IMiddleware
    {
        public class Options : IOptions
        {
            public CookieBuilder Cookie { get; set; } = new CookieBuilder() {
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
                // Let's make a random pause here to make sure that all open windows
                // in the current session don't race to set a new session cookie.
                var delay = RandomStringGenerator.Default.Next().GetHashCode() & 1023;
                await Task.Delay(delay);
                var url = httpContext.Request.GetEncodedPathAndQuery();
                httpContext.Response.Redirect(url);
                // true:  invoke the next middleware
                // false: don't invoke the next middleware
                return false;
            }
        }

        public ISessionProvider SessionProvider { get; }
        public ISessionFactory SessionFactory { get; }
        public IAuthService? AuthService { get; }
        public CookieBuilder Cookie { get; }
        public Func<SessionMiddleware, HttpContext, Task<bool>> ForcedSignOutHandler { get; }

        public SessionMiddleware(
            ISessionProvider sessionProvider,
            ISessionFactory sessionFactory,
            IAuthService? authService = null)
            : this(null, sessionProvider, sessionFactory, authService) { }
        public SessionMiddleware(
            Options? options,
            ISessionProvider sessionProvider,
            ISessionFactory sessionFactory,
            IAuthService? authService = null)
        {
            options ??= new Options();
            Cookie = options.Cookie;
            ForcedSignOutHandler = options.ForcedSignOutHandler;
            SessionProvider = sessionProvider;
            SessionFactory = sessionFactory;
            AuthService = authService;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            var cancellationToken = httpContext.RequestAborted;
            var cookies = httpContext.Request.Cookies;
            var cookieName = Cookie.Name;
            cookies.TryGetValue(cookieName, out var sessionId);
            var session = string.IsNullOrEmpty(sessionId) ? null : new Session(sessionId);
            if (session != null) {
                if (AuthService != null) {
                    var isSignOutForced = await AuthService.IsSignOutForcedAsync(session, cancellationToken);
                    if (isSignOutForced) {
                        if (!await ForcedSignOutHandler(this, httpContext))
                            return;
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
}
