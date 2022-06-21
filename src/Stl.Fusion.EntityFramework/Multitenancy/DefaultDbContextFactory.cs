using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public sealed class DefaultDbContextFactory<TDbContext> : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext> DbContextFactory { get; }

    public DefaultDbContextFactory(IDbContextFactory<TDbContext> dbContextFactory)
        => DbContextFactory = dbContextFactory;

    public TDbContext CreateDbContext(TenantInfo? tenantInfo) 
        => ReferenceEquals(tenantInfo, null)
            ? DbContextFactory.CreateDbContext()
            : throw Errors.DefaultDbContextFactoryDoesNotSupportMultitenancy();
}
