using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

public interface ITenantRegistry
{
    bool IsSingleTenant { get; }
    MutableDictionary<Symbol, Tenant> AllTenants { get; }
    MutableDictionary<Symbol, Tenant> AccessedTenants { get; }

    // Calling this method = get access to a tenant
    public bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant);
}

public interface ITenantRegistry<TContext> : ITenantRegistry
{ }
