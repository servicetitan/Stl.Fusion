using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.Server.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.Server.Authentication;

public class ServerAuthHelper : IHasServices
{
    public record Options
    {
        public string[] IdClaimKeys { get; init; } = { ClaimTypes.NameIdentifier };
        public string[] NameClaimKeys { get; init; } = { ClaimTypes.Name };
        public string CloseWindowRequestPath { get; init; } = "/fusion/close";
        public TimeSpan SessionInfoUpdatePeriod { get; init; } = TimeSpan.FromSeconds(30);
        public bool KeepSignedIn { get; init; }
    }

    protected IAuth Auth { get; }
    protected IAuthBackend AuthBackend { get; }
    protected ISessionResolver SessionResolver { get; }
    protected AuthSchemasCache AuthSchemasCache { get; }
    protected ITenantResolver TenantResolver { get; }
    protected ICommander Commander { get; }
    protected MomentClockSet Clocks { get; }

    public Options Settings { get; }
    public IServiceProvider Services { get; }
    public ILogger Log { get; }
    public Session Session => SessionResolver.Session;

    public ServerAuthHelper(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = services.LogFor(GetType());

        Auth = services.GetRequiredService<IAuth>();
        AuthBackend = services.GetRequiredService<IAuthBackend>();
        SessionResolver = services.GetRequiredService<ISessionResolver>();
        AuthSchemasCache = services.GetRequiredService<AuthSchemasCache>();
        TenantResolver = services.GetRequiredService<ITenantResolver>();
        Commander = services.Commander();
        Clocks = services.Clocks();
    }

    public virtual async ValueTask<string> GetSchemas(HttpContext httpContext, bool cache = true)
    {
        string? schemas;
        if (cache) {
            schemas = AuthSchemasCache.Schemas;
            if (schemas != null)
                return schemas;
        }
        var authSchemas = await httpContext.GetAuthenticationSchemas().ConfigureAwait(false);
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

    public virtual async Task UpdateAuthState(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var httpUser = httpContext.User;
        var httpAuthenticationSchema = httpUser.Identity?.AuthenticationType ?? "";
        var httpIsAuthenticated = !string.IsNullOrEmpty(httpAuthenticationSchema);

        var session = SessionResolver.Session;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var userAgent = httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentValues)
            ? userAgentValues.FirstOrDefault() ?? ""
            : "";

        var sessionInfo = await Auth.GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        var mustUpdateSessionInfo =
            sessionInfo == null
            || !StringComparer.Ordinal.Equals(sessionInfo.IPAddress, ipAddress)
            || !StringComparer.Ordinal.Equals(sessionInfo.UserAgent, userAgent)
            || sessionInfo.LastSeenAt + Settings.SessionInfoUpdatePeriod < Clocks.SystemClock.Now;
        if (mustUpdateSessionInfo) {
            var setupSessionCommand = new SetupSessionCommand(session, ipAddress, userAgent);
            await Commander.Call(setupSessionCommand, cancellationToken).ConfigureAwait(false);
        }

        var user = await Auth.GetUser(session, cancellationToken).ConfigureAwait(false);
        try {
            if (httpIsAuthenticated) {
                if (IsSameUser(user, httpUser, httpAuthenticationSchema))
                    return;
                var (newUser, authenticatedIdentity) = CreateOrUpdateUser(user, httpUser, httpAuthenticationSchema);
                var signInCommand = new SignInCommand(session, newUser, authenticatedIdentity);
                await Commander.Call(signInCommand, cancellationToken).ConfigureAwait(false);
            }
            else if (user != null && !Settings.KeepSignedIn) {
                var signOutCommand = new SignOutCommand(session);
                await Commander.Call(signOutCommand, cancellationToken).ConfigureAwait(false);
            }
        }
        finally {
            // Ideally this should be done once important things are completed
            _ = Task.Run(() => Auth.UpdatePresence(session, default), default);
        }
    }

    public virtual bool IsCloseWindowRequest(HttpContext httpContext, out string closeWindowFlowName)
    {
        var request = httpContext.Request;
        var isCloseWindowRequest = StringComparer.Ordinal.Equals(request.Path.Value, Settings.CloseWindowRequestPath);
        closeWindowFlowName = "";
        if (isCloseWindowRequest && request.Query.TryGetValue("flow", out var flows))
            closeWindowFlowName = flows.FirstOrDefault() ?? "";
        return isCloseWindowRequest;
    }

    // Protected methods

    protected virtual bool IsSameUser(User? user, ClaimsPrincipal httpUser, string schema)
    {
        if (user == null) return false;

        var httpUserIdentityName = httpUser.Identity?.Name ?? "";
        var claims = httpUser.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
        var id = FirstClaimOrDefault(claims, Settings.IdClaimKeys) ?? httpUserIdentityName;
        var identity = new UserIdentity(schema, id);
        return user.Identities.ContainsKey(identity);
    }

    protected virtual (User User, UserIdentity AuthenticatedIdentity) CreateOrUpdateUser(
        User? user, ClaimsPrincipal httpUser, string schema)
    {
        var httpUserIdentityName = httpUser.Identity?.Name ?? "";
        var claims = httpUser.Claims.ToImmutableDictionary(c => c.Type, c => c.Value);
        var id = FirstClaimOrDefault(claims, Settings.IdClaimKeys) ?? httpUserIdentityName;
        var name = FirstClaimOrDefault(claims, Settings.NameClaimKeys) ?? httpUserIdentityName;
        var identity = new UserIdentity(schema, id);
        var identities = ImmutableDictionary<UserIdentity, string>.Empty.Add(identity, "");

        if (user == null)
            // Create
            user = new User("", name) {
                Claims = claims,
                Identities = identities
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
