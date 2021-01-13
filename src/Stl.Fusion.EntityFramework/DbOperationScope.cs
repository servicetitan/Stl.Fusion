using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.Operations;
using Stl.Locking;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationScope : IAsyncDisposable
    {
        public bool IsUsed { get; }
        public bool IsCompleted { get; }

        Task<DbContext> GetDbContextAsync(bool isReadWrite = true, CancellationToken cancellationToken = default);
        Task<IOperation?> CommitAsync(object? command, CancellationToken cancellationToken = default);
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
        protected Moment StartTime { get; }
        protected List<TDbContext> AllDbContexts { get; }
        protected TDbContext? PrimaryDbContext { get; set; }
        protected DbConnection? Connection { get; set; }
        protected IDbContextTransaction? Transaction { get; set; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected IMomentClock Clock { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }
        protected AsyncLock AsyncLock { get; }

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
            AllDbContexts = new List<TDbContext>();
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
                foreach (var dbContext in AllDbContexts) {
                    if (ReferenceEquals(dbContext, PrimaryDbContext))
                        continue;
                    SafeDispose(dbContext);
                }
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
            var dbContext = DbContextFactory.CreateDbContext().ReadWrite(isReadWrite);
            dbContext.Database.AutoTransactionsEnabled = false;
            if (PrimaryDbContext == null) {
                Transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                Connection = dbContext.GetDbConnection();
                if (Connection == null)
                    throw Stl.Internal.Errors.InternalError("No DbConnection.");
                PrimaryDbContext = dbContext;
            }
            else
                dbContext.SetDbConnection(Connection);

            AllDbContexts.Add(dbContext);
            return dbContext;
        }

        public virtual async Task<IOperation?> CommitAsync(object? command, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsCompleted)
                throw Errors.TransactionScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return null;

                foreach (var dbContext in AllDbContexts) {
                    if (!(dbContext.ChangeTracker.AutoDetectChangesEnabled ||
                        dbContext.ChangeTracker.HasChanges()))
                        continue;
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                if (command == null) {
                    await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return null;
                }
                else {
                    var dbContext = PrimaryDbContext!.ReadWrite();
                    dbContext.ChangeTracker.Clear();
                    var operation = await DbOperationLog.AddAsync(dbContext,
                        command, StartTime, Clock.Now,
                        cancellationToken).ConfigureAwait(false);
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
