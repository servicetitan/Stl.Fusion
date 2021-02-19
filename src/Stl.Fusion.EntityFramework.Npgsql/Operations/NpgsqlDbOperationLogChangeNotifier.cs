using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Stl.Async;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.Operations;
using Stl.Locking;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations
{
    public class NpgsqlDbOperationLogChangeNotifier<TDbContext> : DbServiceBase<TDbContext>,
        IOperationCompletionListener, IDisposable
        where TDbContext : DbContext
    {
        public NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> Options { get; }
        protected AgentInfo AgentInfo { get; }
        protected TDbContext? DbContext { get; set; }
        protected AsyncLock AsyncLock { get; }
        protected bool IsDisposed { get; set; }

        public NpgsqlDbOperationLogChangeNotifier(
            NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> options,
            AgentInfo agentInfo,
            IServiceProvider services)
            : base(services)
        {
            Options = options;
            AgentInfo = agentInfo;
            AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed || !disposing)
                return;
            IsDisposed = true;
            using var _ = ExecutionContextEx.SuppressFlow();
            Task.Run(async () => {
                using (await AsyncLock.LockAsync()) {
                    var dbContext = DbContext;
                    if (dbContext != null)
                        await dbContext.DisposeAsync();
                }
            }).Ignore();
        }

        public Task OnOperationCompletedAsync(IOperation operation)
        {
            if (operation.AgentId != AgentInfo.Id.Value) // Only local commands require notification
                return Task.CompletedTask;
            var commandContext = CommandContext.Current;
            if (commandContext != null) { // It's a command
                var operationScope = commandContext.Items.TryGet<DbOperationScope<TDbContext>>();
                if (operationScope == null || !operationScope.IsUsed) // But it didn't change anything related to TDbContext
                    return Task.CompletedTask;
            }
            // If it wasn't command, we pessimistically assume it changed something
            Notify();
            return Task.CompletedTask;
        }

        // Protected methods

        protected virtual void Notify(TimeSpan delay = default)
        {
            using var _ = ExecutionContextEx.SuppressFlow();
            if (delay == default)
                Task.Run(NotifyAsync);
            else
                Task.Delay(delay).ContinueWith(_ => NotifyAsync());
        }

        protected virtual async Task NotifyAsync()
        {
            var qPayload = AgentInfo.Id.Value.Replace("'", "''");
            TDbContext? dbContext = null;
            try {
                using (await AsyncLock.LockAsync()) {
                    if (IsDisposed)
                        throw Errors.AlreadyDisposed();
                    dbContext = DbContext ??= CreateDbContext();
                    await dbContext.Database
                        .ExecuteSqlRawAsync($"NOTIFY {Options.ChannelName}, '{qPayload}'")
                        .ConfigureAwait(false);
                }
            }
            catch {
                DbContext = null;
                Notify(Options.RetryDelay); // Retry
                dbContext?.DisposeAsync().Ignore(); // Doesn't matter if it fails
            }
        }
    }
}
