using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Collections;
using Stl.CommandR.Commands;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Server.Internal;
using Stl.Text;

namespace Stl.Fusion.Server.Authentication
{
    public class ServerAuthHelper
    {
        public class Options
        {
            public Func<ClaimsPrincipal, User>? UserFactory { get; set; } = null;
            public string[] IdClaimKeys { get; set; } = { ClaimTypes.NameIdentifier };
            public string[] NameClaimKeys { get; set; } = { ClaimTypes.Name };
            public string CloseWindowRequestPath { get; set; } = "/fusion/close";
        }

        protected IServerSideAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected AuthSchemasCache AuthSchemasCache { get; }
        protected Func<ClaimsPrincipal, User> UserFactory { get; }
        public string[] IdClaimKeys { get; }
        public string[] NameClaimKeys { get; }
        public string CloseWindowRequestPath { get; }
        public Session Session => SessionResolver.Session;

        public ServerAuthHelper(
            Options? options,
            IServerSideAuthService authService,
            ISessionResolver sessionResolver,
            AuthSchemasCache authSchemasCache)
        {
            options ??= new();
            IdClaimKeys = options.IdClaimKeys;
            NameClaimKeys = options.NameClaimKeys;
            UserFactory = options.UserFactory ?? CreateUser;
            CloseWindowRequestPath = options.CloseWindowRequestPath;

            AuthService = authService;
            SessionResolver = sessionResolver;
            AuthSchemasCache = authSchemasCache;
        }

        public virtual async ValueTask<string> GetSchemasAsync(HttpContext httpContext, bool cache = true)
        {
            string? schemas;
            if (cache) {
                schemas = AuthSchemasCache.Schemas;
                if (schemas != null)
                    return schemas;
            }
            var authSchemas = await httpContext.GetAuthenticationSchemasAsync().ConfigureAwait(false);
            var lSchemas = new List<string>();
            foreach (var authSchema in authSchemas) {
                lSchemas.Add(authSchema.Name);
                lSchemas.Add(authSchema.DisplayName);
            }
            schemas = ListFormat.Default.Format(lSchemas);
            if (cache)
                AuthSchemasCache.Schemas = schemas;
            return schemas;
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
            string? GetFirstClaim(ImmutableDictionary<string, string> claims1, string[] keys)
            {
                foreach (var key in keys) {
                    var v = claims1.GetValueOrDefault(key);
                    if (v != null)
                        return v;
                }
                return null;
            }

            var authenticationType = principal.Identity?.AuthenticationType ?? "";
            var claims = principal.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
            var identityName = principal.Identity?.Name ?? "";
            var id = GetFirstClaim(claims, IdClaimKeys) ?? identityName;
            var name = GetFirstClaim(claims, NameClaimKeys) ?? identityName;
            var user = new User("", name) with {
                Claims = claims,
                Identities = ImmutableDictionary<UserIdentity, string>.Empty
                    .Add((authenticationType, id), "")
            };
            return user;
        }
    }
}
