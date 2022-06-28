using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Multitenancy;

public interface IMultitenantDbContextFactory<out TDbContext>
    where TDbContext : DbContext
{
    TDbContext CreateDbContext(Tenant tenant);
}
