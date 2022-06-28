using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public record MultitenantDbContextFactoryOptions<TDbContext>
    where TDbContext : DbContext
{
    public Func<Tenant, IDbContextFactory<TDbContext>> DbContextFactoryFactory { get; init; } = null!;  
}

public class MultitenantDbContextFactory<TDbContext> : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext> DbContextFactory { get; }

    public MultitenantDbContextFactory(IDbContextFactory<TDbContext> dbContextFactory)
        => DbContextFactory = dbContextFactory;

    public TDbContext CreateDbContext(Tenant tenant)
        => tenant == Tenant.Single
            ? DbContextFactory.CreateDbContext()
            : throw Errors.DefaultDbContextFactoryDoesNotSupportMultitenancy();
}
