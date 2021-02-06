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
        Task<DbContext> CreateDbContextAsync(bool readWrite = true, CancellationToken cancellationToken = default);
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

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            void SafeDispose(IDisposable? d) {
                try {
                    d?.Dispose();
                }
                catch {
                    // Intended
                }
            }

            using var _ = await AsyncLock.LockAsync().ConfigureAwait(false);
            try {
                if (IsUsed && !IsClosed)
                    await RollbackAsync().ConfigureAwait(false);
            }
            finally {
                IsClosed = true;
                SafeDispose(Transaction);
                SafeDispose(DbContext);
            }
        }

        async Task<DbContext> IDbOperationScope.CreateDbContextAsync(bool readWrite, CancellationToken cancellationToken)
            => await CreateDbContextAsync(readWrite, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TDbContext> CreateDbContextAsync(
            bool readWrite = true, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsClosed)
                throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
            TDbContext dbContext;
            if (DbContext == null) {
                dbContext = DbContextFactory.CreateDbContext().ReadWrite();
                dbContext.Database.AutoTransactionsEnabled = false;
                Transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                _isInMemoryProvider = dbContext.Database.ProviderName.EndsWith(".InMemory");
                if (!_isInMemoryProvider) {
                    Connection = dbContext.GetDbConnection();
                    if (Connection == null)
                        throw Stl.Internal.Errors.InternalError("No DbConnection.");
                }
                DbContext = dbContext;
            }
            dbContext = DbContextFactory.CreateDbContext().ReadWrite(readWrite);
            dbContext.Database.AutoTransactionsEnabled = false;
            if (!_isInMemoryProvider)
                dbContext.SetDbConnection(Connection);
            CommandContext.SetOperation(Operation);
            return dbContext;
        }

        public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
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
                var operation = await DbOperationLog.AddAsync(dbContext, Operation, cancellationToken).ConfigureAwait(false);
                await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                operation = await DbOperationLog.TryGetAsync(dbContext, operation.Id, cancellationToken);
                if (operation == null)
                    throw Errors.OperationCommitFailed();
                IsConfirmed = true;
            }
            finally {
                IsConfirmed ??= false;
                IsClosed = true;
            }
        }

        public virtual async Task RollbackAsync()
        {
            using var _ = await AsyncLock.LockAsync().ConfigureAwait(false);
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
