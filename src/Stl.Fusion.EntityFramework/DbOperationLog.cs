using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationLog<in TDbContext>
        where TDbContext : DbContext
    {
        Task<IOperation> AddAsync(TDbContext dbContext,
            object? command, DateTime startTime, DateTime commitTime,
            CancellationToken cancellationToken);
        Task<IOperation?> TryGetAsync(TDbContext dbContext,
            string id, CancellationToken cancellationToken);
        Task<List<IOperation>> ListNewlyCommittedAsync(TDbContext dbContext,
            DateTime minCommitTime, CancellationToken cancellationToken);
    }

    public class DbOperationLog<TDbContext, TDbOperation> : DbServiceBase<TDbContext>, IDbOperationLog<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IOperation, new()
    {
        protected AgentInfo AgentInfo { get; }

        public DbOperationLog(AgentInfo agentInfo, IServiceProvider services)
            : base(services)
            => AgentInfo = agentInfo;

        public virtual async Task<IOperation> AddAsync(
            TDbContext dbContext,
            object? command, DateTime startTime, DateTime commitTime,
            CancellationToken cancellationToken)
        {
            var operation = new TDbOperation() {
                Id = Ulid.NewUlid().ToString(),
                AgentId = AgentInfo.Id.Value,
                StartTime = startTime,
                CommitTime = commitTime,
                Command = command,
            };
            await dbContext.AddAsync((object) operation, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return operation;
        }

        public virtual async Task<IOperation?> TryGetAsync(
            TDbContext dbContext,
            string id, CancellationToken cancellationToken)
            => await dbContext.Set<TDbOperation>().AsQueryable()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

        public async Task<List<IOperation>> ListNewlyCommittedAsync(TDbContext dbContext,
            DateTime minCommitTime, CancellationToken cancellationToken)
        {
            var operations = await dbContext.Set<TDbOperation>().AsQueryable()
                .Where(o => o.CommitTime >= minCommitTime)
                .OrderBy(o => o.CommitTime)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return operations.Cast<IOperation>().ToList();
        }
    }
}
