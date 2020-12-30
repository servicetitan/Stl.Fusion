using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Time;

namespace Stl.EntityFramework
{
    public interface IDbTransactionRunner<out TDbContext>
        where TDbContext : DbContext
    {
        Task ReadOnlyAsync(
            Func<TDbContext, Task> transaction,
            CancellationToken cancellationToken = default);
        Task<string> ReadWriteAsync(
            object operation,
            Func<TDbContext, Task> transaction,
            CancellationToken cancellationToken = default);
    }

    public class DbTransactionRunner<TDbContext, TDbOperation> : IDbTransactionRunner<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IDbOperation, new()
    {
        protected IServiceProvider Services { get; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IMomentClock Clock { get; }

        protected DbTransactionRunner(IServiceProvider services)
        {
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
        }

        public virtual async Task ReadOnlyAsync(
            Func<TDbContext, Task> transaction,
            CancellationToken cancellationToken = default)
        {
            var dbContext = DbContextFactory.CreateDbContext();
            await using var _1 = dbContext.ConfigureAwait(false);
            dbContext.ConfigureMode(DbContextMode.ReadOnly);
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransactionAsync(dbContext,
                async (dbContext1, _) => await transaction.Invoke(dbContext1).ConfigureAwait(false),
                (dbContext1, _) => Task.FromResult(true),
                cancellationToken);
        }

        public virtual async Task<string> ReadWriteAsync(
            object operation,
            Func<TDbContext, Task> transaction,
            CancellationToken cancellationToken = default)
        {
            var dbContext = DbContextFactory.CreateDbContext();
            await using var _ = dbContext.ConfigureAwait(false);
            dbContext.ConfigureMode(DbContextMode.ReadWrite);
            var strategy = dbContext.Database.CreateExecutionStrategy();

            string dbOperationId = "";
            await strategy.ExecuteInTransactionAsync(dbContext,
                async (dbContext1, _) => {
                    var dbOperation = await AddDbOperationAsync(dbContext1, operation, cancellationToken)
                        .ConfigureAwait(false);
                    dbOperationId = dbOperation.Id;
                    await transaction.Invoke(dbContext1).ConfigureAwait(false);
                },
                async (dbContext1, _) => {
                    var dbOperation = await FindDbOperationAsync(dbContext1, dbOperationId, cancellationToken)
                        .ConfigureAwait(false);
                    return dbOperation != null;
                },
                cancellationToken);
            return dbOperationId;
        }

        // Protected methods

        protected virtual async Task<TDbOperation> AddDbOperationAsync(
            TDbContext dbContext, object operation, CancellationToken cancellationToken)
        {
            var dbOperation = new TDbOperation() {
                Id = Ulid.NewUlid().ToString(),
                StartTime = Clock.Now.ToDateTime(),
                Operation = operation,
            };
            await dbContext.AddAsync((object) dbOperation, cancellationToken).ConfigureAwait(false);
            return dbOperation;
        }

        protected virtual Task<TDbOperation?> FindDbOperationAsync(
            TDbContext dbContext, string id, CancellationToken cancellationToken)
            => dbContext.Set<TDbOperation>().SingleOrDefaultAsync(e => e.Id == id, cancellationToken)!;
    }
}
