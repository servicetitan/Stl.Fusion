using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

public class SingleTenantRegistry<TContext> : ITenantRegistry<TContext>
{
    public MutableDictionary<Symbol, Tenant> AllTenants { get; }
    public MutableDictionary<Symbol, Tenant> AccessedTenants { get; }

    public bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant)
        => AccessedTenants.TryGetValue(tenantId, out tenant);

    public SingleTenantRegistry()
    {
        var tenants = new MutableDictionary<Symbol, Tenant>(
            ImmutableDictionary<Symbol, Tenant>.Empty.Add(Tenant.Default.Id, Tenant.Default));
        AllTenants = AccessedTenants = tenants;
    }
}
