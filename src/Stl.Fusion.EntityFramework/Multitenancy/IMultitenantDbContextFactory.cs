using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public interface IMultitenantDbContextFactory<out TDbContext>
    where TDbContext : DbContext
{
    TDbContext CreateDbContext(TenantInfo? tenantInfo);
}
