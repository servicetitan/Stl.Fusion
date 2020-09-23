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
            public Generator<string> IdGenerator { get; set; } = RandomStringGenerator.Default;
            public CookieBuilder Cookie { get; set; } = new CookieBuilder() {
                Name = "FusionAuth.SessionId",
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expiration = TimeSpan.FromDays(28),
            };
            public Func<SessionMiddleware, HttpContext, Task> ForceLogoutHandler { get; set; } = DefaultForceLogoutHandler;

            public static Task DefaultForceLogoutHandler(SessionMiddleware self, HttpContext httpContext)
                => httpContext.SignOutAsync();
        }

        public ISessionProvider SessionProvider { get; }
        public IAuthService? AuthService { get; }
        public Generator<string> IdGenerator { get; }
        public CookieBuilder Cookie { get; }
        public Func<SessionMiddleware, HttpContext, Task> ForceLogoutHandler { get; }

        public SessionMiddleware(ISessionProvider sessionProvider, IAuthService? authService = null)
            : this(null, sessionProvider, authService) { }
        public SessionMiddleware(Options? options, ISessionProvider sessionProvider, IAuthService? authService = null)
        {
            options ??= new Options();
            IdGenerator = options.IdGenerator;
            Cookie = options.Cookie;
            ForceLogoutHandler = options.ForceLogoutHandler;
            SessionProvider = sessionProvider;
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
                    var isLogoutForced = await AuthService.IsLogoutForcedAsync(session, cancellationToken);
                    if (isLogoutForced) {
                        await ForceLogoutHandler(this, httpContext);
                        session = null;
                    }
                }
            }
            if (session == null) {
                sessionId = IdGenerator.Next();
                session = new Session(sessionId);
                var responseCookies = httpContext.Response.Cookies;
                responseCookies.Append(cookieName, sessionId, Cookie.Build(httpContext));
            }
            SessionProvider.Session = session;
            using (session.Activate())
                await next(httpContext);
        }
    }
}
