using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public class MultitenantDbContextFactory<TDbContext> : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public Action<IServiceProvider, Tenant, ServiceCollection> DbServiceCollectionBuilder { get; init; } = null!;
    }

    private ConcurrentDictionary<Symbol, IDbContextFactory<TDbContext>> _factories;

    protected Options Settings { get; }
    protected IServiceProvider Services { get; }

    public MultitenantDbContextFactory(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        _factories = new();
    }

    public TDbContext CreateDbContext(Tenant tenant)
    {
        if (tenant == Tenant.Default)
            throw Errors.DefaultTenantCanBeUsedOnlyWithSingleTenantResolver();
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
