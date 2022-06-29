using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework;

public interface IMultitenantDbContextFactory<out TDbContext>
    where TDbContext : DbContext
{
    TDbContext CreateDbContext(Symbol tenantId);
}
