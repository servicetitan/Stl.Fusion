using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.OS;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationLogger<in TDbContext>
        where TDbContext : DbContext
    {
        Task<string> AddAsync(TDbContext dbContext, object operation, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(TDbContext dbContext, string id, CancellationToken cancellationToken = default);
    }

    public class DbOperationLogger<TDbContext, TDbOperation> : DbServiceBase<TDbContext>, IDbOperationLogger<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IDbOperation, new()
    {
        public DbOperationLogger(IServiceProvider services) : base(services) { }

        public virtual async Task<string> AddAsync(
            TDbContext dbContext, object operation, CancellationToken cancellationToken = default)
        {
            var dbOperation = new TDbOperation() {
                Id = Ulid.NewUlid().ToString(),
                StartTime = Clock.Now.ToDateTime(),
                AgentId = RuntimeInfo.Process.MachinePrefixedId,
                Operation = operation,
            };
            await dbContext.AddAsync((object) dbOperation, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbOperation.Id;
        }

        public virtual async Task<bool> ExistsAsync(
            TDbContext dbContext, string id, CancellationToken cancellationToken = default)
            => null != await dbContext.Set<TDbOperation>()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }
}
