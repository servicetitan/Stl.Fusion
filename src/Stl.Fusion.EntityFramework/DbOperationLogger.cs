using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationLogger<in TDbContext>
        where TDbContext : DbContext
    {
        Task<IDbOperation> AddAsync(TDbContext dbContext, object operation, CancellationToken cancellationToken = default);
        Task<IDbOperation?> TryGetAsync(TDbContext dbContext, string id, CancellationToken cancellationToken = default);
    }

    public class DbOperationLogger<TDbContext, TDbOperation> : DbServiceBase<TDbContext>, IDbOperationLogger<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IDbOperation, new()
    {
        protected AgentInfo AgentInfo { get; }

        public DbOperationLogger(AgentInfo agentInfo, IServiceProvider services)
            : base(services)
            => AgentInfo = agentInfo;

        public virtual async Task<IDbOperation> AddAsync(
            TDbContext dbContext, object operation, CancellationToken cancellationToken = default)
        {
            var dbOperation = new TDbOperation() {
                Id = Ulid.NewUlid().ToString(),
                StartTime = Clock.Now.ToDateTime(),
                AgentId = AgentInfo.Id.Value,
                Operation = operation,
            };
            await dbContext.AddAsync((object) dbOperation, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dbOperation;
        }

        public virtual async Task<IDbOperation?> TryGetAsync(
            TDbContext dbContext, string id, CancellationToken cancellationToken = default)
            => await dbContext.Set<TDbOperation>()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }
}
