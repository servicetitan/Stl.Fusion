using System;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.CommandR.Commands;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Server.Authentication
{
    public interface IAuthSyncHelper
    {
        Task SyncAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
    }

    public class AuthSyncHelper : IAuthSyncHelper
    {
        public class Options
        {
            public string UserNameClaimName { get; set; } = "";
            public Func<ClaimsPrincipal, User>? UserFactory { get; set; } = null;
        }

        protected IServerSideAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected string UserNameClaimName { get; }
        protected Func<ClaimsPrincipal, User>? UserFactory { get; }

        public AuthSyncHelper(
            Options? options,
            IServerSideAuthService authService,
            ISessionResolver sessionResolver)
        {
            options ??= new();
            UserNameClaimName = options.UserNameClaimName;
            UserFactory = options.UserFactory;
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
                user = CreateUser(principal);
                var signInCommand = new SignInCommand(user, session).MarkServerSide();
                await AuthService.SignInAsync(signInCommand, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual User CreateUser(ClaimsPrincipal principal)
        {
            var authenticationType = principal.Identity?.AuthenticationType ?? "";
            var claims = principal.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
            var id = principal.Identity?.Name ?? "";
            var name = string.IsNullOrEmpty(UserNameClaimName)
                ? id
                : claims.GetValueOrDefault(UserNameClaimName) ?? id;
            var user = new User("", name) with {
                Claims = claims,
                Identities = ImmutableDictionary<UserIdentity, string>.Empty
                    .Add((authenticationType, id), "")
            };
            return user;
        }
    }
}
