using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;
using Stl.Locking;
using Stl.Versioning;
using AsyncLock = Stl.Locking.AsyncLock;

namespace Stl.Fusion.EntityFramework;

public interface IDbOperationScope : IOperationScope
{
    DbContext? MasterDbContext { get; }
    DbConnection? Connection { get; }
    IDbContextTransaction? Transaction { get; }
    IsolationLevel IsolationLevel { get; }
    Tenant Tenant { get; }

    Task<DbContext> CreateDbContext(Tenant tenant, bool readWrite = true, CancellationToken cancellationToken = default);
    bool IsTransientFailure(Exception error);
}

public class DbOperationScope<TDbContext> : SafeAsyncDisposableBase, IDbOperationScope
    where TDbContext : DbContext
{
    private bool _isInMemoryProvider;
    private Tenant _tenant = Tenant.Default;

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

    public Tenant Tenant {
        get => _tenant;
        protected set {
            if (_tenant == value)
                return;
            _tenant = !IsUsed ? value : throw Errors.TenantPropertyIsReadOnly();
        }
    }

    // Services
    protected IServiceProvider Services { get; }
    protected IMultitenantDbContextFactory<TDbContext> DbContextFactory { get; }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; }
    protected MomentClockSet Clocks { get; }
    protected AsyncLock AsyncLock { get; }
    protected ILogger Log { get; }

    public DbOperationScope(IServiceProvider services)
    {
        Services = services;
        Log = Services.LogFor(GetType());
        Clocks = Services.Clocks();
        DbContextFactory = Services.GetRequiredService<IMultitenantDbContextFactory<TDbContext>>();
        DbOperationLog = Services.GetRequiredService<IDbOperationLog<TDbContext>>();
        AsyncLock = new AsyncLock(ReentryMode.CheckedPass);
        Operation = DbOperationLog.New();
        CommandContext = Services.GetRequiredService<CommandContext>();
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

    async Task<DbContext> IDbOperationScope.CreateDbContext(Tenant tenant, bool readWrite, CancellationToken cancellationToken)
        => await CreateDbContext(tenant, readWrite, cancellationToken).ConfigureAwait(false);
    public virtual async Task<TDbContext> CreateDbContext(
        Tenant tenant, bool readWrite = true, CancellationToken cancellationToken = default)
    {
        // This code must run in the same execution context to work, so
        // we run it first
        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);
        Tenant = tenant;
        if (IsClosed)
            throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
        if (MasterDbContext == null) {
            // Creating MasterDbContext
            CommandContext.Items.Replace<IOperationScope?>(null, this);
            var masterDbContext = DbContextFactory.CreateDbContext(Tenant).ReadWrite();
            masterDbContext.Database.AutoTransactionsEnabled = false;
#if NET6_0_OR_GREATER
            masterDbContext.Database.AutoSavepointsEnabled = false;
#endif
            Transaction = await BeginTransaction(cancellationToken, masterDbContext).ConfigureAwait(false);
            _isInMemoryProvider = (masterDbContext.Database.ProviderName ?? "")
                .EndsWith(".InMemory", StringComparison.Ordinal);
            if (!_isInMemoryProvider) {
                Connection = masterDbContext.Database.GetDbConnection();
                if (Connection == null)
                    throw Stl.Internal.Errors.InternalError("No DbConnection.");
            }
            MasterDbContext = masterDbContext;
        }
        // Creating requested DbContext, which is going to share MasterDbContext's transaction
        var dbContext = DbContextFactory.CreateDbContext(Tenant).ReadWrite(readWrite);
        dbContext.Database.AutoTransactionsEnabled = false;
#if NET6_0_OR_GREATER
        dbContext.Database.AutoSavepointsEnabled = false;
#endif
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
        if (IsClosed) {
            if (IsConfirmed == true)
                return;
            throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
        }
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
                    var verifierDbContext = DbContextFactory.CreateDbContext(Tenant);
                    await using var _1 = verifierDbContext.ConfigureAwait(false);
                    verifierDbContext.Database.AutoTransactionsEnabled = true;
                    var committedOperation = await DbOperationLog
                        .Get(verifierDbContext, operation.Id, cancellationToken)
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
        if (IsClosed) {
            if (IsConfirmed == false)
                return;
            throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();
        }
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

    public virtual bool IsTransientFailure(Exception error)
    {
        if (error is VersionMismatchException)
            return true;
        if (error is DbUpdateConcurrencyException)
            return true;
        try {
            var executionStrategy = MasterDbContext?.Database.CreateExecutionStrategy();
            if (executionStrategy is not ExecutionStrategy retryingExecutionStrategy)
                return false;
            var isTransient = retryingExecutionStrategy.ShouldRetryOn(error);
            return isTransient;
        }
        catch (ObjectDisposedException) {
            // scope.MasterDbContext?.Database may throw this exception
            try {
                using var altDbContext = DbContextFactory.CreateDbContext(Tenant);
                var executionStrategy = altDbContext.Database.CreateExecutionStrategy();
                if (executionStrategy is not ExecutionStrategy retryingExecutionStrategy)
                    return false;
                var isTransient = retryingExecutionStrategy.ShouldRetryOn(error);
                return isTransient;
            }
            catch {
                // Intended
            }
            return false;
        }
    }

    // Protected methods

    protected virtual Task<IDbContextTransaction> BeginTransaction(
        CancellationToken cancellationToken, TDbContext dbContext)
        => dbContext.Database.BeginTransactionAsync(IsolationLevel, cancellationToken);
}
