using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

// Exposes tenants from any ITenantRegistry to TContext
public class TenantRegistryAlias<TContext> : ITenantRegistry<TContext>
{
    private readonly ITenantRegistry _source;

    public MutableDictionary<Symbol, Tenant> AllTenants 
        => _source.AllTenants;
    public MutableDictionary<Symbol, Tenant> AccessedTenants 
        => _source.AccessedTenants;
    public bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant) 
        => _source.TryGet(tenantId, out tenant);

    public TenantRegistryAlias(ITenantRegistry source) 
        => _source = source;
}

// Exposes tenants from ITenantRegistry<TSourceContext> to TContext
public class TenantRegistryAlias<TSourceContext, TContext> : TenantRegistryAlias<TContext>
{
    public TenantRegistryAlias(ITenantRegistry<TSourceContext> source) : base(source) { } 
}
