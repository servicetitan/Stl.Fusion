using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Generators;

namespace Stl.Fusion.Server.Authentication
{
    [Service(Lifetime = ServiceLifetime.Scoped)]
    public class AuthSessionMiddleware : IMiddleware
    {
        public class Options : IOptions
        {
            public Generator<string> IdGenerator { get; set; } = RandomStringGenerator.Default;
            public CookieBuilder Cookie { get; set; } = new CookieBuilder() {
                Name = "AuthSession",
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expiration = TimeSpan.FromDays(28),
            };
        }

        protected Generator<string> IdGenerator { get; }
        protected CookieBuilder Cookie { get; }
        protected IAuthSessionAccessor AuthSessionAccessor { get; }

        public AuthSessionMiddleware(IAuthSessionAccessor authSessionAccessor)
            : this(null, authSessionAccessor) { }
        public AuthSessionMiddleware(
            Options? options,
            IAuthSessionAccessor authSessionAccessor)
        {
            options ??= new Options();
            IdGenerator = options.IdGenerator;
            Cookie = options.Cookie;
            AuthSessionAccessor = authSessionAccessor;
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
            AuthSessionAccessor.Session = new AuthSession(sessionId);
            await next(httpContext);
        }
    }
}
