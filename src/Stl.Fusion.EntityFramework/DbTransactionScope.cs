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
using Stl.Locking;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbTransactionScope : IAsyncDisposable
    {
        public bool IsUsed { get; }
        public bool IsClosed { get; }

        Task<DbContext> GetDbContextAsync(bool isReadWrite = true, CancellationToken cancellationToken = default);
        Task CommitAsync(object? operation, CancellationToken cancellationToken = default);
        Task RollbackAsync();
    }

    public interface IDbTransactionScope<TDbContext> : IDbTransactionScope
        where TDbContext : DbContext
    {
        new Task<TDbContext> GetDbContextAsync(bool isReadWrite = true, CancellationToken cancellationToken = default);
    }

    public class DbTransactionScope<TDbContext> : AsyncDisposableBase, IDbTransactionScope<TDbContext>
        where TDbContext : DbContext
    {
        protected List<TDbContext> AllDbContexts { get; }
        protected TDbContext? PrimaryDbContext { get; set; }
        protected DbConnection? Connection { get; set; }
        protected IDbContextTransaction? Transaction { get; set; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IServiceProvider Services { get; }
        protected ILogger Log { get; }
        protected AsyncLock AsyncLock { get; }

        public bool IsUsed => PrimaryDbContext != null;
        public bool IsClosed { get; private set; }

        public DbTransactionScope(IServiceProvider services)
        {
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            AllDbContexts = new List<TDbContext>();
            AsyncLock = new AsyncLock(ReentryMode.CheckedPass);
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
                foreach (var dbContext in AllDbContexts) {
                    if (ReferenceEquals(dbContext, PrimaryDbContext))
                        continue;
                    SafeDispose(dbContext);
                }
                SafeDispose(Transaction);
                SafeDispose(PrimaryDbContext);
            }
        }

        async Task<DbContext> IDbTransactionScope.GetDbContextAsync(bool isReadWrite, CancellationToken cancellationToken)
            => await GetDbContextAsync(isReadWrite, cancellationToken).ConfigureAwait(false);

        public virtual async Task<TDbContext> GetDbContextAsync(
            bool isReadWrite = true, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsClosed)
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

        public virtual async Task CommitAsync(object? operation, CancellationToken cancellationToken = default)
        {
            using var _ = await AsyncLock.LockAsync(cancellationToken).ConfigureAwait(false);
            if (IsClosed)
                throw Errors.TransactionScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return;

                foreach (var dbContext in AllDbContexts) {
                    if (!(dbContext.ChangeTracker.AutoDetectChangesEnabled ||
                        dbContext.ChangeTracker.HasChanges()))
                        continue;
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                if (operation != null) {
                    var opManager = Services.GetRequiredService<IDbOperationLogger<TDbContext>>();
                    var dbContext = PrimaryDbContext!.ReadWrite();
                    dbContext.ChangeTracker.Clear();
                    await opManager.AddAsync(dbContext, operation, cancellationToken).ConfigureAwait(false);
                }

                await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally {
                IsClosed = true;
            }
        }

        public virtual async Task RollbackAsync()
        {
            using var _ = await AsyncLock.LockAsync().ConfigureAwait(false);
            if (IsClosed)
                throw Errors.TransactionScopeIsAlreadyClosed();
            try {
                if (!IsUsed)
                    return;
                await Transaction!.RollbackAsync().ConfigureAwait(false);
            }
            finally {
                IsClosed = true;
            }
        }
    }
}
