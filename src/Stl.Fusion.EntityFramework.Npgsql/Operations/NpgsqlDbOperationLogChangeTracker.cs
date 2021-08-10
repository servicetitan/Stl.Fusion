using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Stl.Async;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations
{
    public class NpgsqlDbOperationLogChangeTracker<TDbContext> : DbWakeSleepProcessBase<TDbContext>,
        IDbOperationLogChangeTracker<TDbContext>
        where TDbContext : DbContext
    {
        public NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> Options { get; }
        protected AgentInfo AgentInfo { get; }
        protected Task<Unit> NextEventTask { get; set; } = null!;
        protected object Lock { get; } = new();

        public NpgsqlDbOperationLogChangeTracker(
            NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> options,
            AgentInfo agentInfo,
            IServiceProvider services)
            : base(services)
        {
            Options = options;
            AgentInfo = agentInfo;
            // ReSharper disable once VirtualMemberCallInConstructor
            ReplaceNextEventTask();
        }

        public Task WaitForChanges(CancellationToken cancellationToken = default)
        {
            lock (Lock) {
                var task = NextEventTask;
                if (NextEventTask.IsCompleted)
                    ReplaceNextEventTask();
                return task;
            }
        }

        // Protected methods

        protected override async Task WakeUp(CancellationToken cancellationToken)
        {
            await using var dbContext = CreateDbContext();
            var database = dbContext.Database;
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var dbConnection = (NpgsqlConnection) database.GetDbConnection()!;
            dbConnection.Notification += (_, eventArgs) => {
                if (eventArgs.Payload != AgentInfo.Id)
                    ReleaseWaitForChanges();
            };
            await dbContext.Database
                .ExecuteSqlRawAsync($"LISTEN " + Options.ChannelName, cancellationToken)
                .ConfigureAwait(false);
            while (!cancellationToken.IsCancellationRequested)
                await dbConnection.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override Task Sleep(Exception? error, CancellationToken cancellationToken)
            => error != null
                ? Clocks.CoarseCpuClock.Delay(Options.RetryDelay, cancellationToken)
                : Task.CompletedTask;

        protected virtual void ReleaseWaitForChanges()
        {
            lock (Lock)
                TaskSource.For(NextEventTask).TrySetResult(default);
        }

        protected virtual void ReplaceNextEventTask()
            => NextEventTask = TaskSource.New<Unit>(false).Task;
    }
}
