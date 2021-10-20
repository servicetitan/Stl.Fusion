using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Locking;

namespace Stl.Fusion.EntityFramework;

public interface IDbOperationScope : IOperationScope
{
    DbContext? MasterDbContext { get; }
    DbConnection? Connection { get; }
    IDbContextTransaction? Transaction { get; }
    IsolationLevel IsolationLevel { get; }

    Task<DbContext> CreateDbContext(bool readWrite = true, CancellationToken cancellationToken = default);
}

public class DbOperationScope<TDbContext> : SafeAsyncDisposableBase, IDbOperationScope
    where TDbContext : DbContext
{
    private bool _isInMemoryProvider;

    DbContext? IDbOperationScope.MasterDbContext => MasterDbContext;
    public TDbContext? MasterDbContext { get; protected set; }
    public DbConnection? Connection { get; protected set; }
    public IDbContextTransaction? Transaction { get; protected set; }
    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.Unspecified;

    IOperation IOperationScope.Operation => Operation;
    public DbOperation Operation { get; protected init; }
    public CommandContext CommandContext { get; protected init; }
    public bool IsUsed => MasterDbContext != null;
    public bool IsClosed { get; private set; }
    public bool? IsConfirmed { get; private set; }

    protected IDbContextFactory<TDbContext> DbContextFactory { get; init; }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; init; }
    protected MomentClockSet Clocks { get; init; }
    protected AsyncLock AsyncLock { get; init; }
    protected IServiceProvider Services { get; init; }
    protected ILogger Log { get; init; }

    public DbOperationScope(IServiceProvider services)
    {
        var loggerFactory = services.GetService<ILoggerFactory>();
        Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        Services = services;
        Clocks = services.Clocks();
        DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
        DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
        AsyncLock = new AsyncLock(ReentryMode.CheckedPass);
        Operation = DbOperationLog.New();
        CommandContext = services.GetRequiredService<CommandContext>();
    }

    protected override async Task DisposeAsync(bool disposing)
    {
        // Intentionally ignore disposing flag here

        using var _ = await AsyncLock.Lock().ConfigureAwait(false);
        try {
            if (IsUsed && !IsClosed)
                await Rollback().ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.LogWarning(e, "DisposeAsync: error on rollback");
        }
        finally {
            IsClosed = true;
            SilentDispose(Transaction);
            SilentDispose(MasterDbContext);
        }

        void SilentDispose(IDisposable? d) {
            try {
                d?.Dispose();
            }
            catch {
                // Intended
            }
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
        if (MasterDbContext == null) {
            // Creating MasterDbContext
            var masterDbContext = DbContextFactory.CreateDbContext().ReadWrite();
            masterDbContext.Database.AutoTransactionsEnabled = false;
            Transaction = await BeginTransaction(cancellationToken, masterDbContext).ConfigureAwait(false);
            _isInMemoryProvider = (masterDbContext.Database.ProviderName ?? "").EndsWith(".InMemory");
            if (!_isInMemoryProvider) {
                Connection = masterDbContext.Database.GetDbConnection();
                if (Connection == null)
                    throw Stl.Internal.Errors.InternalError("No DbConnection.");
            }
            MasterDbContext = masterDbContext;
        }
        // Creating requested DbContext, which is going to share MasterDbContext's transaction
        var dbContext = DbContextFactory.CreateDbContext().ReadWrite(readWrite);
        dbContext.Database.AutoTransactionsEnabled = false;
        if (!_isInMemoryProvider) {
            dbContext.StopPooling();
            dbContext.Database.SetDbConnection(Connection);
            await dbContext.Database.UseTransactionAsync(Transaction!.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
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

            Operation.CommitTime = Clocks.SystemClock.Now;
            if (Operation.Command == null)
                throw Stl.Fusion.Operations.Internal.Errors.OperationHasNoCommand();
            var masterDbContext = MasterDbContext!;
            masterDbContext.DisableChangeTracking(); // Just to speed up things a bit
            var operation = await DbOperationLog
                .Add(masterDbContext, Operation, cancellationToken)
                .ConfigureAwait(false);
            try {
                await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                IsConfirmed = true;
            }
            catch (Exception) {
                // See https://docs.microsoft.com/en-us/ef/ef6/fundamentals/connection-resiliency/commit-failures
                try {
                    // We need a new connection here, since the old one might be broken
                    masterDbContext = DbContextFactory.CreateDbContext();
                    masterDbContext.Database.AutoTransactionsEnabled = true;
                    var committedOperation = await DbOperationLog
                        .TryGet(masterDbContext, operation.Id, cancellationToken)
                        .ConfigureAwait(false);
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

    // Protected methods

    protected virtual Task<IDbContextTransaction> BeginTransaction(
        CancellationToken cancellationToken, TDbContext dbContext)
        => dbContext.Database.BeginTransactionAsync(IsolationLevel, cancellationToken);
}
