using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public sealed class SingleTenantDbContextFactory<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TDbContext>(IDbContextFactory<TDbContext> dbContextFactory)
    : IMultitenantDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext> DbContextFactory { get; } = dbContextFactory;

    public TDbContext CreateDbContext(Symbol tenantId)
        => tenantId == Tenant.Default
            ? DbContextFactory.CreateDbContext()
            : throw Errors.NonDefaultTenantIsUsedInSingleTenantMode();
}
