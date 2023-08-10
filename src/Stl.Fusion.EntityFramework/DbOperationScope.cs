using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;
using Stl.Locking;

namespace Stl.Fusion.EntityFramework;

public interface IDbOperationScope : IOperationScope
{
    DbContext? MasterDbContext { get; }
    DbConnection? Connection { get; }
    IDbContextTransaction? Transaction { get; }
    string? TransactionId { get; }
    IsolationLevel IsolationLevel { get; set; }
    Tenant Tenant { get; }

    Task<DbContext> InitializeDbContext(DbContext preCreatedDbContext, Tenant tenant, CancellationToken cancellationToken = default);
    bool IsTransientFailure(Exception error);
}

public class DbOperationScope<TDbContext> : SafeAsyncDisposableBase, IDbOperationScope
    where TDbContext : DbContext
{
    public record Options
    {
        public IsolationLevel DefaultIsolationLevel { get; init; } = IsolationLevel.Unspecified;
    }

    private Tenant _tenant = Tenant.Default;

    public Options Settings { get; protected init; }
    DbContext? IDbOperationScope.MasterDbContext => MasterDbContext;
    public TDbContext? MasterDbContext { get; protected set; }
    public DbConnection? Connection { get; protected set; }
    public IDbContextTransaction? Transaction { get; protected set; }
    public string? TransactionId { get; protected set; }
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.Unspecified;

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

            _tenant = !IsUsed ? value : throw Errors.TenantCannotBeChanged();
        }
    }

    // Services
    protected IServiceProvider Services { get; }
    protected ITenantRegistry<TDbContext> TenantRegistry { get; }
    protected IMultitenantDbContextFactory<TDbContext> DbContextFactory { get; }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; }
    protected TransactionIdGenerator<TDbContext> TransactionIdGenerator { get; }
    protected MomentClockSet Clocks { get; }
    protected ReentrantAsyncLock AsyncLock { get; }
    protected ILogger Log { get; }

    public DbOperationScope(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = Services.LogFor(GetType());
        Clocks = Services.Clocks();
        TenantRegistry = Services.GetRequiredService<ITenantRegistry<TDbContext>>();
        DbContextFactory = Services.GetRequiredService<IMultitenantDbContextFactory<TDbContext>>();
        DbOperationLog = Services.GetRequiredService<IDbOperationLog<TDbContext>>();
        TransactionIdGenerator = Services.GetRequiredService<TransactionIdGenerator<TDbContext>>();
        AsyncLock = new ReentrantAsyncLock(LockReentryMode.CheckedPass);
        Operation = DbOperationLog.New();
        CommandContext = CommandContext.GetCurrent();
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

    async Task<DbContext> IDbOperationScope.InitializeDbContext(
        DbContext dbContext, Tenant tenant, CancellationToken cancellationToken)
        => await InitializeDbContext((TDbContext) dbContext, tenant, cancellationToken).ConfigureAwait(false);
    public virtual async Task<TDbContext> InitializeDbContext(
        TDbContext dbContext, Tenant tenant, CancellationToken cancellationToken = default)
    {
        // This code must run in the same execution context to work, so
        // we run it first
        using var _ = await AsyncLock.Lock(cancellationToken).ConfigureAwait(false);
        Tenant = tenant;
        if (IsClosed)
            throw Stl.Fusion.Operations.Internal.Errors.OperationScopeIsAlreadyClosed();

        if (MasterDbContext == null)
            await CreateMasterDbContext(cancellationToken).ConfigureAwait(false);

        var database = dbContext.Database;
        database.DisableAutoTransactionsAndSavepoints();
        if (Connection != null) {
            dbContext.SuppressDispose();
            database.SetDbConnection(Connection);
            await database.UseTransactionAsync(Transaction!.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
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

            var dbContext = MasterDbContext!;
            dbContext.EnableChangeTracking(false); // Just to speed up things a bit
            var operation = await DbOperationLog
                .Add(dbContext, Operation, cancellationToken)
                .ConfigureAwait(false);
            try {
                await Transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                IsConfirmed = true;
                Log.IfEnabled(LogLevel.Debug)?.LogDebug("Transaction #{TransactionId}: committed", TransactionId);
            }
            catch (Exception) {
                // See https://docs.microsoft.com/en-us/ef/ef6/fundamentals/connection-resiliency/commit-failures
                try {
                    // We need a new connection here, since the old one might be broken
                    var verifierDbContext = DbContextFactory.CreateDbContext(Tenant);
                    await using var _1 = verifierDbContext.ConfigureAwait(false);
#if NET7_0_OR_GREATER
                    verifierDbContext.Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;
#else
                    verifierDbContext.Database.AutoTransactionsEnabled = true;
#endif
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
            Log.IfEnabled(LogLevel.Debug)?.LogDebug("Transaction #{TransactionId}: rolled back", TransactionId);
            IsConfirmed ??= false;
            IsClosed = true;
        }
    }

    public virtual bool IsTransientFailure(Exception error)
    {
        if (error is ITransientException)
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
        catch (ObjectDisposedException e) {
            // scope.MasterDbContext?.Database may throw this exception
            Log.LogWarning(e, "IsTransientFailure resorts to temporary {DbContext}", typeof(TDbContext).Name);
            try {
                var tenantId = TenantRegistry.IsSingleTenant ? Tenant.Default.Id : Tenant.Dummy.Id;
                using var tmpDbContext = DbContextFactory.CreateDbContext(tenantId);
                var executionStrategy = tmpDbContext.Database.CreateExecutionStrategy();
                if (executionStrategy is not ExecutionStrategy retryingExecutionStrategy)
                    return false;
                var isTransient = retryingExecutionStrategy.ShouldRetryOn(error);
                return isTransient;
            }
            catch (Exception e2) {
                Log.LogWarning(e2, "IsTransientFailure fails for temporary {DbContext}", typeof(TDbContext).Name);
                // Intended
            }
            return false;
        }
    }

    // Protected methods

    protected virtual async Task CreateMasterDbContext(CancellationToken cancellationToken)
    {
        var dbContext = DbContextFactory.CreateDbContext(Tenant).ReadWrite();
        var database = dbContext.Database;
        database.DisableAutoTransactionsAndSavepoints();

        var commandContext = CommandContext;
        // 1. If IsolationLevel is set explicitly, we honor it
        var isolationLevel = IsolationLevel;
        if (isolationLevel != IsolationLevel.Unspecified)
            goto ready;

        // 2. Try to query DbContext-specific DbIsolationLevelSelectors
        var selectors = Services.GetRequiredService<IEnumerable<DbIsolationLevelSelector<TDbContext>>>();
        isolationLevel = selectors
            .Select(s => s.GetCommandIsolationLevel(commandContext))
            .Aggregate(IsolationLevel.Unspecified, (x, y) => x.Max(y));
        if (isolationLevel != IsolationLevel.Unspecified)
            goto ready;

        // 3. Try to query GlobalIsolationLevelSelectors
        var globalSelectors = Services.GetRequiredService<IEnumerable<GlobalIsolationLevelSelector>>();
        isolationLevel = globalSelectors
            .Select(s => s.GetCommandIsolationLevel(commandContext))
            .Aggregate(IsolationLevel.Unspecified, (x, y) => x.Max(y));
        if (isolationLevel != IsolationLevel.Unspecified)
            goto ready;

        // 4. Use Settings.DefaultIsolationLevel
        isolationLevel = Settings.DefaultIsolationLevel;

        ready:
        IsolationLevel = isolationLevel;
        Transaction = await database
            .BeginTransactionAsync(isolationLevel, cancellationToken)
            .ConfigureAwait(false);
        TransactionId = TransactionIdGenerator.Next();
        if (!database.IsInMemory()) {
            Connection = database.GetDbConnection();
            if (Connection == null)
                throw Stl.Internal.Errors.InternalError("No DbConnection.");
        }
        MasterDbContext = dbContext;
        CommandContext.Items.Replace<IOperationScope?>(null, this);

        Log.IfEnabled(LogLevel.Debug)?.LogDebug(
            "Transaction #{TransactionId}: started @ IsolationLevel = {IsolationLevel}",
            TransactionId, isolationLevel);
    }
}
