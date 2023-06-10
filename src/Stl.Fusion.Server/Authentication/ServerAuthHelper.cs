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
        public static Options Default { get; set; } = new();

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

    public Task UpdateAuthState(HttpContext httpContext, CancellationToken cancellationToken = default)
        => UpdateAuthState(SessionResolver.Session, httpContext, cancellationToken);

    public virtual async Task UpdateAuthState(
        Session session,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var httpUser = httpContext.User;
        var httpAuthenticationSchema = httpUser.Identity?.AuthenticationType ?? "";
        var httpIsAuthenticated = !httpAuthenticationSchema.IsNullOrEmpty();

        var ipAddress = httpContext.GetRemoteIPAddress()?.ToString() ?? "";
        var userAgent = httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentValues)
            ? userAgentValues.FirstOrDefault() ?? ""
            : "";

        var sessionInfo = await GetSessionInfo(session, cancellationToken).ConfigureAwait(false);
        var mustSetupSession =
            sessionInfo == null
            || !StringComparer.Ordinal.Equals(sessionInfo.IPAddress, ipAddress)
            || !StringComparer.Ordinal.Equals(sessionInfo.UserAgent, userAgent)
            || sessionInfo.LastSeenAt + Settings.SessionInfoUpdatePeriod < Clocks.SystemClock.Now;
        if (mustSetupSession || sessionInfo == null)
            sessionInfo = await SetupSession(session, sessionInfo, ipAddress, userAgent, cancellationToken)
                .ConfigureAwait(false);

        var user = await GetUser(session, sessionInfo, cancellationToken).ConfigureAwait(false);
        try {
            if (httpIsAuthenticated) {
                if (IsSameUser(user, httpUser, httpAuthenticationSchema))
                    return;
                await SignIn(session, sessionInfo, user, httpUser, httpAuthenticationSchema, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (user != null && !Settings.KeepSignedIn) {
                await SignOut(session, sessionInfo, cancellationToken).ConfigureAwait(false);
            }
        }
        finally {
            // This should be done once important things are completed
            await UpdatePresence(session, sessionInfo, cancellationToken).ConfigureAwait(false);
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

    protected virtual Task<SessionInfo?> GetSessionInfo(Session session, CancellationToken cancellationToken)
        => Auth.GetSessionInfo(session, cancellationToken);

    protected virtual Task<User?> GetUser(
        Session session, SessionInfo sessionInfo,
        CancellationToken cancellationToken)
        => Auth.GetUser(session, cancellationToken);

    protected virtual Task<SessionInfo> SetupSession(
        Session session, SessionInfo? sessionInfo, string ipAddress, string userAgent,
        CancellationToken cancellationToken)
    {
        var setupSessionCommand = new SetupSessionCommand(session, ipAddress, userAgent);
        return Commander.Call(setupSessionCommand, true, cancellationToken);
    }

    protected virtual Task SignIn(
        Session session, SessionInfo sessionInfo,
        User? user, ClaimsPrincipal httpUser, string httpAuthenticationSchema,
        CancellationToken cancellationToken)
    {
        var (newUser, authenticatedIdentity) = CreateOrUpdateUser(user, httpUser, httpAuthenticationSchema);
        var signInCommand = new SignInCommand(session, newUser, authenticatedIdentity);
        return Commander.Call(signInCommand, true, cancellationToken);
    }

    protected virtual Task SignOut(
        Session session, SessionInfo sessionInfo,
        CancellationToken cancellationToken)
    {
        var signOutCommand = new SignOutCommand(session);
        return Commander.Call(signOutCommand, true, cancellationToken);
    }

    protected virtual Task UpdatePresence(
        Session session, SessionInfo sessionInfo,
        CancellationToken cancellationToken)
    {
        _ = Auth.UpdatePresence(session, CancellationToken.None);
        return Task.CompletedTask;
    }

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
            user = new User(Symbol.Empty, name) {
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
            if (claims.TryGetValue(key, out var value) && !value.IsNullOrEmpty())
                return value;
        return null;
    }
}
