using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Operations;

public interface IDbOperationLogChangeTracker<TDbContext>
    where TDbContext : DbContext
{
    Task WaitForChanges(CancellationToken cancellationToken = default);
}
