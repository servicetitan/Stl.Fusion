using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Operations;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationLog<in TDbContext>
        where TDbContext : DbContext
    {
        IOperation New(string? id = null, string? agentId = null, object? command = null);
        Task<IOperation> AddAsync(TDbContext dbContext, IOperation operation, CancellationToken cancellationToken);
        Task<IOperation?> TryGetAsync(TDbContext dbContext, string id, CancellationToken cancellationToken);

        Task<List<IOperation>> ListNewlyCommittedAsync(DateTime minCommitTime, CancellationToken cancellationToken);
        Task TrimAsync(DateTime minCommitTime, CancellationToken cancellationToken);
    }

    public class DbOperationLog<TDbContext, TDbOperation> : DbServiceBase<TDbContext>, IDbOperationLog<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IOperation, new()
    {
        protected AgentInfo AgentInfo { get; }

        public DbOperationLog(IServiceProvider services)
            : base(services)
        {
            AgentInfo = services.GetRequiredService<AgentInfo>();
        }

        public IOperation New(string? id = null, string? agentId = null, object? command = null)
            => new TDbOperation() {
                Id = id ?? Ulid.NewUlid().ToString(),
                AgentId = agentId ?? AgentInfo.Id.Value,
                StartTime = Clock.Now,
                Command = command,
            };

        public virtual async Task<IOperation> AddAsync(TDbContext dbContext,
            IOperation operation, CancellationToken cancellationToken)
        {
            // dbContext shouldn't use tracking!
            var dbOperation = (TDbOperation) operation;
            await dbContext.AddAsync((object) dbOperation, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return operation;
        }

        public virtual async Task<IOperation?> TryGetAsync(TDbContext dbContext,
            string id, CancellationToken cancellationToken)
            // dbContext shouldn't use tracking!
            => await dbContext.Set<TDbOperation>().AsQueryable()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

        public async Task<List<IOperation>> ListNewlyCommittedAsync(
            DateTime minCommitTime, CancellationToken cancellationToken)
        {
            await using var dbContext = CreateDbContext();
            var operations = await dbContext.Set<TDbOperation>().AsQueryable()
                .Where(o => o.CommitTime >= minCommitTime)
                .OrderBy(o => o.CommitTime)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return operations.Cast<IOperation>().ToList();
        }

        public async Task TrimAsync(DateTime minCommitTime, CancellationToken cancellationToken)
        {
            await using var dbContext = CreateDbContext();
            dbContext.DisableChangeTracking();
            for (;;) {
                var operations = await dbContext.Set<TDbOperation>().AsQueryable()
                    .Where(o => o.CommitTime < minCommitTime)
                    .OrderBy(o => o.CommitTime)
                    .Take(1000)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (operations.Count == 0)
                    break;
                foreach (var operation in operations)
                    dbContext.Remove(operation);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
