using System.Collections.Immutable;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR.Commands;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Templates.Blazor2.Host.Services
{
    [Service(Lifetime = ServiceLifetime.Scoped)]
    public class AuthSyncService
    {
        protected IServerSideAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }

        public AuthSyncService(
            IServerSideAuthService authService,
            ISessionResolver sessionResolver)
        {
            AuthService = authService;
            SessionResolver = sessionResolver;
        }

        public virtual async Task SyncAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var principal = httpContext.User;
            var session = SessionResolver.Session;

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "";
            var setupSessionCommand = new SetupSessionCommand(ipAddress, userAgent, session).MarkServerSide();
            var sessionInfo = AuthService.SetupSessionAsync(setupSessionCommand, cancellationToken).ConfigureAwait(false);

            var user = await AuthService.GetUserAsync(session, cancellationToken).ConfigureAwait(false);
            if (((IPrincipal) user).Identity?.Name == principal.Identity?.Name)
                return;

            var authenticationType = principal.Identity?.AuthenticationType ?? "";
            if (authenticationType == "") {
                await AuthService.SignOutAsync(new(false, session), cancellationToken).ConfigureAwait(false);
            }
            else {
                var claims = principal.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
                var id = principal.Identity?.Name ?? "";
                var name = claims.GetValueOrDefault(GitHubAuthenticationConstants.Claims.Name) ?? "";
                user = new User("", name) with {
                    Claims = claims,
                    Identities = ImmutableDictionary<UserIdentity, string>.Empty
                        .Add((authenticationType, id), "")
                };
                var signInCommand = new SignInCommand(user, session).MarkServerSide();
                await AuthService.SignInAsync(signInCommand, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
