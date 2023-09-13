using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Internal;

public class FuncDbContextFactory<TDbContext>(Func<TDbContext> factory) : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext()
        => factory.Invoke();
}
