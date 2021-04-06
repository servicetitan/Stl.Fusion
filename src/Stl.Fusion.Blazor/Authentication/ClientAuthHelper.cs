using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Text;

namespace Stl.Fusion.Blazor
{
    public class ClientAuthHelper
    {
        public static string SchemasJavaScriptExpression { get; set; } = "window.FusionAuth.schemas";

        protected (string Schema, string SchemaName)[]? CachedSchemas { get; set; }
        protected IJSRuntime JSRuntime { get; }

        public IAuthService AuthService { get; }
        public ISessionResolver SessionResolver { get; }
        public Session Session => SessionResolver.Session;

        public ClientAuthHelper(
            IAuthService authService,
            ISessionResolver sessionResolver,
            IJSRuntime jsRuntime)
        {
            AuthService = authService;
            SessionResolver = sessionResolver;
            JSRuntime = jsRuntime;
        }

        public virtual async ValueTask<(string Name, string DisplayName)[]> GetSchemas()
        {
            if (CachedSchemas != null)
                return CachedSchemas;
            var sSchemas = await JSRuntime.InvokeAsync<string>("eval", SchemasJavaScriptExpression);
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
        public virtual Task SignOut(Symbol sessionId, bool force = false)
            => SignOut(new Session(sessionId), force);
        public virtual async Task SignOut(Session session, bool force = false)
        {
            var signOutCommand = new SignOutCommand(session, force);
            await Task.Run(() => AuthService.SignOut(signOutCommand));
        }

        public virtual async Task SignOutEverywhere(bool force = true)
        {
            // No server-side API endpoint for this action(yet), so let's do this on the client
            for (;;) {
                var sessionInfos = await AuthService.GetUserSessions(Session);
                var otherSessions = sessionInfos
                    .Where(si => si.Id != Session.Id)
                    .Select(si => new Session(si.Id))
                    .ToList();
                if (otherSessions.Count == 0)
                    break;
                foreach (var otherSession in otherSessions)
                    await SignOut(otherSession, force);
            }
            await SignOut(Session, force);
        }
    }
}
