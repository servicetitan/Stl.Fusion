using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Multitenancy;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public interface IMultitenantDbContextFactory<out TDbContext>
    where TDbContext : DbContext
{
    TDbContext CreateDbContext(TenantInfo? tenantInfo);
}
