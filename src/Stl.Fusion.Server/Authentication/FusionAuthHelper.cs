using System;
using System.Collections.Immutable;
using System.Linq;
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
    public class FusionAuthHelper
    {
        public class Options
        {
            public Func<ClaimsPrincipal, User>? UserFactory { get; set; } = null;
            public string IdClaimKey { get; set; } = ClaimTypes.NameIdentifier;
            public string NameClaimKey { get; set; } = ClaimTypes.Name;
            public string CloseWindowRequestPath { get; set; } = "/fusion/close";
        }

        protected IServerSideAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected Func<ClaimsPrincipal, User> UserFactory { get; }
        public string IdClaimKey { get; }
        public string NameClaimKey { get; }
        public string CloseWindowRequestPath { get; }
        public Session Session => SessionResolver.Session;

        public FusionAuthHelper(
            Options? options,
            IServerSideAuthService authService,
            ISessionResolver sessionResolver)
        {
            options ??= new();
            IdClaimKey = options.IdClaimKey;
            NameClaimKey = options.NameClaimKey;
            UserFactory = options.UserFactory ?? CreateUser;
            CloseWindowRequestPath = options.CloseWindowRequestPath;

            AuthService = authService;
            SessionResolver = sessionResolver;
        }

        public virtual async Task UpdateAuthStateAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var principal = httpContext!.User;
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
                user = UserFactory(principal);
                var signInCommand = new SignInCommand(user, session).MarkServerSide();
                await AuthService.SignInAsync(signInCommand, cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual bool IsCloseWindowRequest(HttpContext httpContext, out string closeWindowFlowName)
        {
            var request = httpContext.Request;
            var isCloseWindowRequest = request.Path.Value == CloseWindowRequestPath;
            closeWindowFlowName = "";
            if (isCloseWindowRequest && request.Query.TryGetValue("flow", out var flows))
                closeWindowFlowName = flows.FirstOrDefault() ?? "";
            return isCloseWindowRequest;
        }

        // Protected methods

        protected virtual User CreateUser(ClaimsPrincipal principal)
        {
            var authenticationType = principal.Identity?.AuthenticationType ?? "";
            var claims = principal.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
            var identityName = principal.Identity?.Name ?? "";
            var id = claims.GetValueOrDefault(IdClaimKey) ?? identityName;
            var name = claims.GetValueOrDefault(NameClaimKey) ?? identityName;
            var user = new User("", name) with {
                Claims = claims,
                Identities = ImmutableDictionary<UserIdentity, string>.Empty
                    .Add((authenticationType, id), "")
            };
            return user;
        }
    }
}
