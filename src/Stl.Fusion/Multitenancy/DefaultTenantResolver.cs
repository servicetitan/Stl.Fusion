using Stl.Fusion.Authentication;
using Stl.Multitenancy;

namespace Stl.Fusion.Multitenancy;

public class DefaultTenantResolver<TContext> : ITenantResolver<TContext>
{
    public record Options
    {
        public string TenantIdFieldName { get; init; } = "TenantId";
        public string TenantIdOptionName { get; init; } = "TenantId";
    }

    protected Options Settings { get; }
    protected IServiceProvider Services { get; }
    protected ITenantRegistry<TContext> TenantRegistry { get; }
    protected IAuth Auth { get; }

    public DefaultTenantResolver(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        TenantRegistry = services.GetRequiredService<ITenantRegistry<TContext>>();
        Auth = services.GetRequiredService<IAuth>();
    }

    public virtual async Task<Tenant> Resolve(object source, object context, CancellationToken cancellationToken)
    {
        switch (source) {
        case Session session:
            var options = await Auth.GetOptions(session, cancellationToken).ConfigureAwait(false);
            var sTenantId = options.GetOrDefault(Settings.TenantIdOptionName);
            return sTenantId.IsNullOrEmpty() ? Tenant.Default : TenantRegistry.Get(sTenantId);
        case ICommand command:
            object? oTenantId = null;
            try {
                oTenantId = command.GetType().GetField(Settings.TenantIdFieldName)?.GetValue(command);
            }
            catch {
                // Intended
            }
            if (oTenantId is Symbol tenantId)
                return TenantRegistry.Get(tenantId);
            if (oTenantId is string sTenantId1)
                return TenantRegistry.Get(sTenantId1);
            if (command is ISessionCommand sessionCommand)
                return await Resolve(sessionCommand.Session, context, cancellationToken).ConfigureAwait(false);
            return Tenant.Default;
        default:
            return Tenant.Default;
        }
    }
}
