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

    protected Options Settings { get; init; }
    protected IServiceProvider Services { get; }
    protected ITenantRegistry<TContext> TenantRegistry { get; }
    protected IAuth Auth { get; }

    public DefaultTenantResolver(Options? settings, IServiceProvider services)
    {
        Settings = settings ?? new();
        Services = services;
        TenantRegistry = services.GetRequiredService<ITenantRegistry<TContext>>();
        Auth = services.GetRequiredService<IAuth>();
    }

    public virtual async Task<Tenant> Resolve(Session session, CancellationToken cancellationToken = default)
    {
        var options = await Auth.GetOptions(session, cancellationToken).ConfigureAwait(false);
        var tenantId = options.GetOrDefault(Settings.TenantIdOptionName);
        return tenantId.IsNullOrEmpty() ? Tenant.Single : TenantRegistry.Get(tenantId);
    }

    public virtual async Task<Tenant> Resolve(ICommand command, CancellationToken cancellationToken = default)
    {
        object? oTenantId = null;
        try {
            oTenantId = command.GetType().GetField(Settings.TenantIdFieldName)?.GetValue(command);
        }
        catch {
            // Intended
        }
        if (oTenantId is Symbol tenantId)
            return TenantRegistry.Get(tenantId);
        if (oTenantId is string sTenantId)
            return TenantRegistry.Get(sTenantId);
        if (command is ISessionCommand sessionCommand)
            return await Resolve(sessionCommand.Session, cancellationToken).ConfigureAwait(false);
        return Tenant.Single;
    }
}
