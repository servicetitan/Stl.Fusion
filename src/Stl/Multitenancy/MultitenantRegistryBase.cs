using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

public abstract class MultitenantRegistryBase<TContext> : ITenantRegistry<TContext>
{
    protected readonly object Lock = new();

    public MutableDictionary<Symbol, Tenant> AllTenants { get; }
    public MutableDictionary<Symbol, Tenant> AccessedTenants { get; }

    protected MultitenantRegistryBase()
    {
        var tenants = ImmutableDictionary<Symbol, Tenant>.Empty;
        AllTenants = new MutableDictionary<Symbol, Tenant>(tenants);
        AccessedTenants = new MutableDictionary<Symbol, Tenant>(tenants);
        Task.Run(Update);
    }

    public virtual bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant)
    {
        if (AccessedTenants.TryGetValue(tenantId, out tenant))
            return true;
        lock (Lock) {
            // Double check locking
            if (AccessedTenants.TryGetValue(tenantId, out tenant))
                return true;

            // Let's see if we have this item in AllTenants first
            if (AllTenants.TryGetValue(tenantId, out tenant)) {
                AccessedTenants.Items = AccessedTenants.Items.Add(tenantId, tenant);
                return true;
            }

            // Ok, we have to resort to fetch
            if (AllTenants.SetItems(FetchAllTenants()) && AllTenants.TryGetValue(tenantId, out tenant)) {
                AccessedTenants.Items = AccessedTenants.Items.Add(tenantId, tenant);
                return true;
            }
        }
        return false;
    }

    public virtual void Remove(Symbol tenantId)
    {
        lock (Lock) {
            AccessedTenants.Items = AccessedTenants.Items.Remove(tenantId);
            AllTenants.Items = AllTenants.Items.Remove(tenantId);
        }
    }

    public virtual void Update()
    {
        lock (Lock) {
            // We lock here also to maintain the proper order of updates
            AllTenants.Items = FetchAllTenants();
        }
    }

    // Protected methods

    protected abstract ImmutableDictionary<Symbol, Tenant> FetchAllTenants();
}
