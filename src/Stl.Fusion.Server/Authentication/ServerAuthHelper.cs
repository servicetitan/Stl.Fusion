using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
            public string[] IdClaimKeys { get; set; } = { ClaimTypes.NameIdentifier };
            public string[] NameClaimKeys { get; set; } = { ClaimTypes.Name };
            public string CloseWindowRequestPath { get; set; } = "/fusion/close";
        }

        protected IServerSideAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected AuthSchemasCache AuthSchemasCache { get; }
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
                lSchemas.Add(authSchema.DisplayName ?? authSchema.Name);
            }
            schemas = ListFormat.Default.Format(lSchemas);
            if (cache)
                AuthSchemasCache.Schemas = schemas;
            return schemas;
        }

        public virtual async Task UpdateAuthStateAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var httpUser = httpContext.User;
            var httpAuthenticationSchema = httpUser.Identity?.AuthenticationType ?? "";
            var isAuthenticated = !string.IsNullOrEmpty(httpAuthenticationSchema);

            var session = SessionResolver.Session;
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentValues)
                ? userAgentValues.FirstOrDefault() ?? ""
                : "";
            var setupSessionCommand = new SetupSessionCommand(session, ipAddress, userAgent).MarkServerSide();
            var sessionInfo = await AuthService.SetupSessionAsync(setupSessionCommand, cancellationToken).ConfigureAwait(false);
            var userId = sessionInfo.UserId;
            var userIsAuthenticated = sessionInfo.IsAuthenticated && !sessionInfo.IsSignOutForced;
            var user = userIsAuthenticated
                ? (await AuthService.TryGetUserAsync(userId, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException())
                : new User(session.Id); // Guest

            if (isAuthenticated) {
                if (userIsAuthenticated && IsSameUser(user, httpUser, httpAuthenticationSchema))
                    return;
                var (newUser, authenticatedIdentity) = CreateOrUpdateUser(user, httpUser, httpAuthenticationSchema);
                var signInCommand = new SignInCommand(session, newUser, authenticatedIdentity).MarkServerSide();
                await AuthService.SignInAsync(signInCommand, cancellationToken).ConfigureAwait(false);
            }
            else {
                if (userIsAuthenticated) {
                    var signOutCommand = new SignOutCommand(session);
                    await AuthService.SignOutAsync(signOutCommand, cancellationToken).ConfigureAwait(false);
                }
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

        protected virtual bool IsSameUser(User user, ClaimsPrincipal httpUser, string schema)
        {
            var httpUserIdentityName = httpUser.Identity?.Name ?? "";
            var claims = httpUser.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
            var id = FirstClaimOrDefault(claims, IdClaimKeys) ?? httpUserIdentityName;
            var identity = new UserIdentity(schema, id);
            return user.Identities.ContainsKey(identity);
        }

        protected virtual (User User, UserIdentity AuthenticatedIdentity) CreateOrUpdateUser(
            User user, ClaimsPrincipal httpUser, string schema)
        {
            var httpUserIdentityName = httpUser.Identity?.Name ?? "";
            var claims = httpUser.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
            var id = FirstClaimOrDefault(claims, IdClaimKeys) ?? httpUserIdentityName;
            var name = FirstClaimOrDefault(claims, NameClaimKeys) ?? httpUserIdentityName;
            var identity = new UserIdentity(schema, id);
            var identities = ImmutableDictionary<UserIdentity, string>.Empty.Add(identity, "");

            if (!user.IsAuthenticated)
                // Create
                user = new User("", name) with {
                    Claims = claims,
                    Identities = identities,
                };
            else {
                // Update
                user = user with {
                    Claims = claims.SetItems(user.Claims),
                    Identities = identities,
                };
            }
            return (user, identity);
        }

        protected static string? FirstClaimOrDefault(IReadOnlyDictionary<string, string> claims, string[] keys)
        {
            foreach (var key in keys)
                if (claims.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                    return value;
            return null;
        }
    }
}
