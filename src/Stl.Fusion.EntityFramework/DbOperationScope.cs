using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Collections;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.Operations;
using Stl.Locking;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationScope : IAsyncDisposable
    {
        Moment StartTime { get; set; }
        object? Command { get; set; }
        ImmutableOptionSet Items { get; set; }
        bool IsUsed { get; }
        bool IsCompleted { get; }
        bool? IsConfirmed { get; }

        Task<DbContext> GetDbContextAsync(bool isReadWrite = true, CancellationToken cancellationToken = default);
        Task<IOperation?> CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync();
    }

    public interface IDbOperationScope<TDbContext> : IDbOperationScope
        where TDbContext : DbContext
    {
        new Task<TDbContext> GetDbContextAsync(bool isReadWrite = true, CancellationToken cancellationToken = default);
    }

    public class DbOperationScope<TDbContext> : AsyncDisposableBase, IDbOperationScope<TDbContext>
        where TDbContext : DbContext
    {
        private bool _isInMemoryProvider;

        protected TDbContext? PrimaryDbContext { get; set; }
        protected DbConnection? Connection { get; set; }
        protected IDbContextTransaction? Transaction { get; set; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }
        protected AsyncLock AsyncLock { get; }

        public Moment StartTime { get; set; }
        public object? Command { get; set; }
        public ImmutableOptionSet Items { get; set; }
        public bool IsUsed => PrimaryDbContext != null;
        public bool IsCompleted { get; private set; }
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
            StartTime = Clock.Now;
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
                if (IsUsed && !IsCompleted)
                    await RollbackAsync().ConfigureAwait(false);
            }
            finally {
                IsCompleted = true;
                SafeDispose(Transaction);
                SafeDispose(PrimaryDbContext);
            }
        }

        async Task<DbContext> IDbOperationScope.GetDbContextAsync(bool isReadWrite, CancellationToken cancellationToken)
            => await GetDbContextAsync(isReadWrite, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TDbContext> GetDbContextAsync(
            bool isReadWrite = true, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsCompleted)
                throw Errors.TransactionScopeIsAlreadyClosed();
            TDbContext dbContext;
            if (PrimaryDbContext == null) {
                dbContext = DbContextFactory.CreateDbContext().ReadWrite();
                dbContext.Database.AutoTransactionsEnabled = false;
                Transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                _isInMemoryProvider = dbContext.Database.ProviderName.EndsWith(".InMemory");
                if (!_isInMemoryProvider) {
                    Connection = dbContext.GetDbConnection();
                    if (Connection == null)
                        throw Stl.Internal.Errors.InternalError("No DbConnection.");
                }
                PrimaryDbContext = dbContext;
            }
            dbContext = DbContextFactory.CreateDbContext().ReadWrite(isReadWrite);
            dbContext.Database.AutoTransactionsEnabled = false;
            if (!_isInMemoryProvider)
                dbContext.SetDbConnection(Connection);
            return dbContext;
        }

        public virtual async Task<IOperation?> CommitAsync(CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsCompleted)
                throw Errors.TransactionScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return null;

                if (Command == null) {
                    await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return null;
                }
                else {
                    var dbContext = PrimaryDbContext!;
                    var operation = await DbOperationLog.AddAsync(dbContext, o => {
                            o.StartTime = StartTime;
                            o.CommitTime = Clock.Now;
                            o.Command = Command;
                            o.Items = Items;
                        }, cancellationToken).ConfigureAwait(false);
                    await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    operation = await DbOperationLog.TryGetAsync(dbContext, operation.Id, cancellationToken);
                    if (operation == null)
                        throw Errors.OperationCommitFailed();
                    return operation;
                }
            }
            finally {
                IsCompleted = true;
            }
        }

        public virtual async Task RollbackAsync()
        {
            using var _ = await AsyncLock.LockAsync().ConfigureAwait(false);
            if (IsCompleted)
                throw Errors.TransactionScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return;
                await Transaction!.RollbackAsync().ConfigureAwait(false);
            }
            finally {
                IsCompleted = true;
            }
        }
    }
}
