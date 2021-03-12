using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Operations;
using Stl.Locking;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationScope : IOperationScope
    {
        Task<DbContext> CreateDbContext(bool readWrite = true, CancellationToken cancellationToken = default);
    }

    public class DbOperationScope<TDbContext> : AsyncDisposableBase, IDbOperationScope
        where TDbContext : DbContext
    {
        private bool _isInMemoryProvider;

        protected TDbContext? DbContext { get; set; }
        protected DbConnection? Connection { get; set; }
        protected IDbContextTransaction? Transaction { get; set; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected AsyncLock AsyncLock { get; }
        protected ILogger Log { get; }

        IOperation IOperationScope.Operation => Operation;
        public DbOperation Operation { get; }
        public CommandContext CommandContext { get; }
        public bool IsUsed => DbContext != null;
        public bool IsClosed { get; private set; }
        public bool? IsConfirmed { get; private set; }

        public DbOperationScope(IServiceProvider services)
        {
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
            Services = services;
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
            AsyncLock = new AsyncLock(ReentryMode.CheckedPass);
            Operation = DbOperationLog.New();
            CommandContext = services.GetRequiredService<CommandContext>();
        }

        protected override async ValueTask DisposeInternal(bool disposing)
        {
            void SafeDispose(IDisposable? d) {
                try {
                    d?.Dispose();
                }
                catch {
                    // Intended
                }
            }

            using var _ = await AsyncLock.Lock().ConfigureAwait(false);
            try {
                if (IsUsed && !IsClosed)
                    await Rollback().ConfigureAwait(false);
            }
            finally {
                IsClosed = true;
                SafeDispose(Transaction);
                SafeDispose(DbContext);
            }
        }

        async Task<DbContext> IDbOperationScope.CreateDbContext(bool readWrite, CancellationToken cancellationToken)
            => await CreateDbContext(readWrite, cancellationToken).ConfigureAwait(false);
        public virtual async Task<TDbContext> CreateDbContext(
            bool readWrite = true, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);
            if (IsClosed)
                throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
            TDbContext dbContext;
            if (DbContext == null) {
                dbContext = DbContextFactory.CreateDbContext().ReadWrite();
                dbContext.Database.AutoTransactionsEnabled = false;
                Transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                _isInMemoryProvider = dbContext.Database.ProviderName.EndsWith(".InMemory");
                if (!_isInMemoryProvider) {
                    Connection = dbContext.Database.GetDbConnection();
                    if (Connection == null)
                        throw Stl.Internal.Errors.InternalError("No DbConnection.");
                }
                DbContext = dbContext;
            }
            dbContext = DbContextFactory.CreateDbContext().ReadWrite(readWrite);
            dbContext.Database.AutoTransactionsEnabled = false;
            if (!_isInMemoryProvider) {
                dbContext.StopPooling();
                dbContext.Database.SetDbConnection(Connection);
            }
            CommandContext.SetOperation(Operation);
            return dbContext;
        }

        public virtual async Task Commit(CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);
            if (IsClosed)
                throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
            try {
                if (!IsUsed) {
                    IsConfirmed = true;
                    return;
                }

                Operation.CommitTime = Clock.Now;
                if (Operation.Command == null)
                    throw Stl.Fusion.Operations.Internal.Errors.OperationHasNoCommand();
                var dbContext = DbContext!;
                dbContext.DisableChangeTracking(); // Just to speed up things a bit
                var operation = await DbOperationLog.Add(dbContext, Operation, cancellationToken).ConfigureAwait(false);
                try {
                    await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    IsConfirmed = true;
                }
                catch (Exception) {
                    // See https://docs.microsoft.com/en-us/ef/ef6/fundamentals/connection-resiliency/commit-failures
                    try {
                        // We need a new connection here, since the old one might be broken
                        dbContext = DbContextFactory.CreateDbContext();
                        var committedOperation = await DbOperationLog.TryGet(dbContext, operation.Id, cancellationToken);
                        if (committedOperation != null)
                            IsConfirmed = true;
                    }
                    catch {
                        // Intended
                    }
                    if (IsConfirmed != true)
                        throw;
                }
            }
            finally {
                IsConfirmed ??= false;
                IsClosed = true;
            }
        }

        public virtual async Task Rollback()
        {
            using var _ = await AsyncLock.Lock().ConfigureAwait(false);
            if (IsClosed)
                throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return;
                await Transaction!.RollbackAsync().ConfigureAwait(false);
            }
            finally {
                IsConfirmed ??= false;
                IsClosed = true;
            }
        }
    }
}
