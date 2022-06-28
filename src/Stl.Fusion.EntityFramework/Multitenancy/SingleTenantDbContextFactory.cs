using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public sealed class SingleTenantDbContextFactory<TDbContext> : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext> DbContextFactory { get; }

    public SingleTenantDbContextFactory(IDbContextFactory<TDbContext> dbContextFactory)
        => DbContextFactory = dbContextFactory;

    public TDbContext CreateDbContext(Tenant tenant)
        => tenant == Tenant.Single
            ? DbContextFactory.CreateDbContext()
            : throw Errors.DefaultDbContextFactoryDoesNotSupportMultitenancy();
}
