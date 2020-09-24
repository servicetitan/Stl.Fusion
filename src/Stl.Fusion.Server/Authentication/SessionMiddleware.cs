using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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
            public Func<SessionMiddleware, HttpContext, Task> ForcedSignOutHandler { get; set; } =
                DefaultForcedSignOutHandler;

            public static Task DefaultForcedSignOutHandler(SessionMiddleware self, HttpContext httpContext)
                => httpContext.SignOutAsync();
        }

        public ISessionProvider SessionProvider { get; }
        public ISessionFactory SessionFactory { get; }
        public IAuthService? AuthService { get; }
        public CookieBuilder Cookie { get; }
        public Func<SessionMiddleware, HttpContext, Task> ForcedSignOutHandler { get; }

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
                        await ForcedSignOutHandler(this, httpContext);
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
