using Microsoft.JSInterop;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Blazor;

public class ClientAuthHelper : IHasServices
{
    public static string SchemasJavaScriptExpression { get; set; } = "window.FusionAuth.schemas";

    private IAuth? _auth;
    private ISessionResolver? _sessionResolver;
    private ICommander? _commander;
    private IJSRuntime? _jsRuntime;

    protected (string Schema, string SchemaName)[]? CachedSchemas { get; set; }

    public IServiceProvider Services { get; }
    public IAuth Auth => _auth ??= Services.GetRequiredService<IAuth>();
    public ISessionResolver SessionResolver => _sessionResolver ??= Services.GetRequiredService<ISessionResolver>();
    public Session Session => SessionResolver.Session;
    public ICommander Commander => _commander ??= Services.Commander();
    public IJSRuntime JSRuntime => _jsRuntime ??= Services.GetRequiredService<IJSRuntime>();

    public ClientAuthHelper(IServiceProvider services)
        => Services = services;

    public virtual async ValueTask<(string Name, string DisplayName)[]> GetSchemas()
    {
        if (CachedSchemas != null)
            return CachedSchemas;

        var sSchemas = await JSRuntime
            .InvokeAsync<string>("eval", SchemasJavaScriptExpression)
            .ConfigureAwait(false); // The rest of this method doesn't depend on Blazor
        var lSchemas = ListFormat.Default.Parse(sSchemas);
        var schemas = new (string, string)[lSchemas.Count / 2];
        for (int i = 0, j = 0; i < schemas.Length; i++, j += 2)
            schemas[i] = (lSchemas[j], lSchemas[j + 1]);
        CachedSchemas = schemas;
        return CachedSchemas;
    }

    public virtual ValueTask SignIn(string? schema = null)
        => JSRuntime.InvokeVoidAsync("FusionAuth.signIn", schema ?? "");

    public virtual ValueTask SignOut()
        => JSRuntime.InvokeVoidAsync("FusionAuth.signOut");
    public virtual Task SignOut(Session session, bool force = false)
        => Commander.Call(new SignOutCommand(session, force));
    public virtual Task SignOutEverywhere(bool force = true)
        => Commander.Call(new SignOutCommand(Session, force) { KickAllUserSessions = true });
    public virtual Task Kick(Session session, string otherSessionHash, bool force = false)
        => Commander.Call(new SignOutCommand(session, otherSessionHash, force));
}
