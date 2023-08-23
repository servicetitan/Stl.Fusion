using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

// Exposes tenants from any ITenantRegistry to TContext
public class TenantRegistryAlias<TContext>(ITenantRegistry source) : ITenantRegistry<TContext>
{
    public bool IsSingleTenant => source.IsSingleTenant;
    public MutableDictionary<Symbol, Tenant> AllTenants => source.AllTenants;
    public MutableDictionary<Symbol, Tenant> AccessedTenants => source.AccessedTenants;

    public bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant)
        => source.TryGet(tenantId, out tenant);
}

// Exposes tenants from ITenantRegistry<TSourceContext> to TContext
public class TenantRegistryAlias<TSourceContext, TContext>(ITenantRegistry<TSourceContext> source)
    : TenantRegistryAlias<TContext>(source);
