using System.Diagnostics.CodeAnalysis;

namespace Stl.Multitenancy;

public class MultitenantRegistry<TContext> : ITenantRegistry<TContext>
{
    public record Options
    {
        public Func<ImmutableDictionary<Symbol, Tenant>> AllTenantsFetcher { get; init; } = null!;
    }

    protected readonly object Lock = new();

    public Options Settings { get; }
    public bool IsSingleTenant => false;
    public MutableDictionary<Symbol, Tenant> AllTenants { get; }
    public MutableDictionary<Symbol, Tenant> AccessedTenants { get; }

    public MultitenantRegistry(Options settings)
    {
        Settings = settings;
        var tenants = ImmutableDictionary<Symbol, Tenant>.Empty;
        AllTenants = new MutableDictionary<Symbol, Tenant>(tenants);
        AccessedTenants = new MutableDictionary<Symbol, Tenant>(tenants);
        // ReSharper disable once VirtualMemberCallInConstructor
        Update();
    }

    public virtual bool TryGet(Symbol tenantId, [MaybeNullWhen(false)] out Tenant tenant)
    {
        if (tenantId == Tenant.Default.Id) {
            tenant = null;
            return false;
        }

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

    protected virtual ImmutableDictionary<Symbol, Tenant> FetchAllTenants()
        => Settings.AllTenantsFetcher.Invoke();
}
