using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Operations
{
    public interface IDbOperationLogChangeTracker<TDbContext>
        where TDbContext : DbContext
    {
        Task WaitForChanges(CancellationToken cancellationToken = default);
    }
}
