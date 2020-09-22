using System;
using System.Threading.Tasks;
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
        }

        protected ISessionProvider SessionProvider { get; }
        protected Generator<string> IdGenerator { get; }
        protected CookieBuilder Cookie { get; }

        public SessionMiddleware(ISessionProvider sessionProvider) : this(null, sessionProvider) { }
        public SessionMiddleware(Options? options, ISessionProvider sessionProvider)
        {
            options ??= new Options();
            IdGenerator = options.IdGenerator;
            Cookie = options.Cookie;
            SessionProvider = sessionProvider;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            var cookies = httpContext.Request.Cookies;
            var cookieName = Cookie.Name;
            var hasCookie = cookies.TryGetValue(cookieName, out var sessionId);
            if (!hasCookie || string.IsNullOrEmpty(sessionId)) {
                sessionId = IdGenerator.Next();
                var responseCookies = httpContext.Response.Cookies;
                if (hasCookie)
                    responseCookies.Delete(cookieName);
                responseCookies.Append(cookieName, sessionId, Cookie.Build(httpContext));
            }
            var session = new Session(sessionId);
            SessionProvider.Session = session;
            using (session.Activate())
                await next(httpContext);
        }
    }
}
