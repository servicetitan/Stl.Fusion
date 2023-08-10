using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public class MultitenantDbContextFactory<TDbContext>(
        MultitenantDbContextFactory<TDbContext>.Options settings,
        IServiceProvider services
        ) : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public Action<IServiceProvider, Tenant, ServiceCollection> DbServiceCollectionBuilder { get; init; } = null!;
    }

    private readonly ConcurrentDictionary<Symbol, IDbContextFactory<TDbContext>> _factories = new();
    private ITenantRegistry<TDbContext>? _tenantRegistry;

    protected Options Settings { get; } = settings;
    protected IServiceProvider Services { get; } = services;
    protected ITenantRegistry<TDbContext> TenantRegistry // Let's avoid possible cycle
        => _tenantRegistry ??= Services.GetRequiredService<ITenantRegistry<TDbContext>>();

    public TDbContext CreateDbContext(Symbol tenantId)
    {
        if (tenantId == Tenant.Default)
            throw Errors.DefaultTenantCanOnlyBeUsedInSingleTenantMode();
        var tenant = TenantRegistry.Get(tenantId);
        var factory = GetDbContextFactory(tenant);
        return factory.CreateDbContext();
    }

    // Protected methods

    protected virtual IDbContextFactory<TDbContext> GetDbContextFactory(Tenant tenant)
        => _factories.GetOrAdd(tenant.Id, static (_, state) => {
            var (self, tenant1) = state;
            var services = new ServiceCollection();
            self.Settings.DbServiceCollectionBuilder.Invoke(self.Services, tenant1, services);
            var serviceProvider = services.BuildServiceProvider();
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
            return dbContextFactory;
        }, (Self: this, Tenant: tenant));
}
