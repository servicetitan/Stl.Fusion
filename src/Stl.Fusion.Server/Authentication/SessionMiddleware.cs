using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Generators;

namespace Stl.Fusion.Server.Authentication
{
    [Service(Lifetime = ServiceLifetime.Scoped)]
    public class SessionMiddleware : IMiddleware
    {
        public class Options : IOptions
        {
            public Generator<string> IdGenerator { get; set; } = RandomStringGenerator.Default;
            public string HttpSessionKey { get; set; } = "Session";
        }

        protected Generator<string> IdGenerator { get; }
        protected string HttpSessionKey { get; }
        protected ISessionAccessor SessionAccessor { get; }

        public SessionMiddleware(ISessionAccessor sessionAccessor)
            : this(null, sessionAccessor) { }
        public SessionMiddleware(
            Options? options,
            ISessionAccessor sessionAccessor)
        {
            options ??= new Options();
            IdGenerator = options.IdGenerator;
            HttpSessionKey = options.HttpSessionKey;
            SessionAccessor = sessionAccessor;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            var session = httpContext.Session;
            await session.LoadAsync().ConfigureAwait(false);
            var sessionId = session.GetString(HttpSessionKey);
            if (string.IsNullOrEmpty(sessionId)) {
                sessionId = IdGenerator.Next();
                session.SetString(HttpSessionKey, sessionId);
            }
            SessionAccessor.Session = new Session(sessionId);
            await next(httpContext);
        }
    }
}
