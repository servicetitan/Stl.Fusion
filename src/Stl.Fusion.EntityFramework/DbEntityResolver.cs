using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Multitenancy;
using Stl.Net;

namespace Stl.Fusion.EntityFramework;

public interface IDbEntityResolver<TKey, TDbEntity>
    where TKey : notnull
    where TDbEntity : class
{
    Func<TDbEntity, TKey> KeyExtractor { get; init; }
    Expression<Func<TDbEntity, TKey>> KeyExtractorExpression { get; init; }

    Task<TDbEntity?> Get(Symbol tenantId, TKey key, CancellationToken cancellationToken = default);
}

/// <summary>
/// This type queues (when needed) & batches calls to <see cref="Get"/> with
/// <see cref="BatchProcessor{TIn,TOut}"/> to reduce the rate of underlying DB queries.
/// </summary>
/// <typeparam name="TDbContext">The type of <see cref="DbContext"/>.</typeparam>
/// <typeparam name="TKey">The type of entity key.</typeparam>
/// <typeparam name="TDbEntity">The type of entity to pipeline batch for.</typeparam>
public class DbEntityResolver<TDbContext, TKey, TDbEntity> : DbServiceBase<TDbContext>,
    IDbEntityResolver<TKey, TDbEntity>,
    IAsyncDisposable
    where TDbContext : DbContext
    where TKey : notnull
    where TDbEntity : class
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public Expression<Func<TDbEntity, TKey>>? KeyExtractor { get; init; }
        public Expression<Func<IQueryable<TDbEntity>, IQueryable<TDbEntity>>>? QueryTransformer { get; init; }
        public Action<Dictionary<TKey, TDbEntity>> PostProcessor { get; init; } = _ => { };
        public int BatchSize { get; init; } = 14; // Max. EF.CompileQuery parameter count = 15
        public Action<BatchProcessor<TKey, TDbEntity?>>? ConfigureBatchProcessor { get; init; }
        public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(1);
        public IRetryDelayer RetryDelayer { get; init; } = new RetryDelayer() {
            Delays = RetryDelaySeq.Exp(0.125, 0.5, 0.1, 2),
            Limit = 3,
        };
    }

    // ReSharper disable once StaticMemberInGenericType
    protected static MethodInfo DbContextSetMethod { get; } = typeof(DbContext)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(m => Equals(m.Name, nameof(DbContext.Set)) && m.IsGenericMethod && m.GetParameters().Length == 0)
        .MakeGenericMethod(typeof(TDbEntity));
    protected static MethodInfo QueryableWhereMethod { get; }
        = new Func<IQueryable<TDbEntity>, Expression<Func<TDbEntity, bool>>, IQueryable<TDbEntity>>(Queryable.Where).Method;

    private ConcurrentDictionary<Symbol, BatchProcessor<TKey, TDbEntity?>>? _batchProcessors;
    private ITransientErrorDetector<TDbContext>? _transientErrorDetector;

    protected Options Settings { get; }
    protected (Func<TDbContext, TKey[], IAsyncEnumerable<TDbEntity>> Query, int BatchSize)[] Queries { get; init; }

    public Func<TDbEntity, TKey> KeyExtractor { get; init; }
    public Expression<Func<TDbEntity, TKey>> KeyExtractorExpression { get; init; }
    public ITransientErrorDetector<TDbContext> TransientErrorDetector =>
        _transientErrorDetector ??= Services.GetRequiredService<ITransientErrorDetector<TDbContext>>();

    public DbEntityResolver(Options settings, IServiceProvider services) : base(services)
    {
        Settings = settings;
        var keyExtractor = Settings.KeyExtractor;
        if (keyExtractor == null) {
            var dummyTenant = TenantRegistry.IsSingleTenant ? Tenant.Default : Tenant.Dummy;
            using var dbContext = CreateDbContext(dummyTenant);
            var keyPropertyName = dbContext.Model
                .FindEntityType(typeof(TDbEntity))!
                .FindPrimaryKey()!
                .Properties.Single().Name;

            var pEntity = Expression.Parameter(typeof(TDbEntity), "e");
            var eBody = Expression.PropertyOrField(pEntity, keyPropertyName);
            keyExtractor = Expression.Lambda<Func<TDbEntity, TKey>>(eBody, pEntity);
        }
        KeyExtractorExpression = keyExtractor;
        KeyExtractor = keyExtractor.Compile();
        _batchProcessors = new();

        var buffer = ArrayBuffer<(Func<TDbContext, TKey[], IAsyncEnumerable<TDbEntity>>, int)>.Lease(false);
        try {
            for (var batchSize = 2; batchSize < Settings.BatchSize; batchSize *= 2)
                buffer.Add((CreateCompiledQuery(batchSize), batchSize));
            buffer.Add((CreateCompiledQuery(Settings.BatchSize), Settings.BatchSize));
            Queries = buffer.ToArray();
        }
        finally {
            buffer.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        var batchProcessors = Interlocked.Exchange(ref _batchProcessors, null);
        if (batchProcessors == null)
            return;
        await batchProcessors.Values
            .Select(p => p.DisposeAsync().AsTask())
            .Collect()
            .ConfigureAwait(false);
    }

    public virtual Task<TDbEntity?> Get(Symbol tenantId, TKey key, CancellationToken cancellationToken = default)
    {
        var batchProcessor = GetBatchProcessor(tenantId);
        return batchProcessor.Process(key, cancellationToken)!;
    }

    // Protected methods

    protected Func<TDbContext, TKey[], IAsyncEnumerable<TDbEntity>> CreateCompiledQuery(int batchSize)
    {
        var pDbContext = Expression.Parameter(typeof(TDbContext), "dbContext");
        var pKeys = new ParameterExpression[batchSize];
        for (var i = 0; i < batchSize; i++)
            pKeys[i] = Expression.Parameter(typeof(TKey), $"key{i.ToString(CultureInfo.InvariantCulture)}");
        var pEntity = Expression.Parameter(typeof(TDbEntity), "e");

        // entity.Key expression
        var eKey = KeyExtractorExpression.Body.Replace(KeyExtractorExpression.Parameters[0], pEntity);

        // .Where predicate expression
        var ePredicate = (Expression?)null;
        for (var i = 0; i < batchSize; i++) {
            var eCondition = Expression.Equal(eKey, pKeys[i]);
            ePredicate = ePredicate == null ? eCondition : Expression.OrElse(ePredicate, eCondition);
        }
        var lPredicate = Expression.Lambda<Func<TDbEntity, bool>>(ePredicate!, pEntity);

        // dbContext.Set<TDbEntity>().Where(...)
        var eEntitySet = Expression.Call(pDbContext, DbContextSetMethod);
        var eWhere = Expression.Call(null, QueryableWhereMethod, eEntitySet, Expression.Quote(lPredicate));

        // Applying QueryTransformer
        var qt = Settings.QueryTransformer;
        var eBody = qt == null
            ? eWhere
            : qt.Body.Replace(qt.Parameters[0], eWhere);

        // Creating compiled query
        var lambdaParameters = new ParameterExpression[batchSize + 1];
        lambdaParameters[0] = pDbContext;
        pKeys.CopyTo(lambdaParameters, 1);
        var lambda = Expression.Lambda(eBody, lambdaParameters);
#pragma warning disable EF1001
        var query = new CompiledAsyncEnumerableQuery<TDbContext, TDbEntity>(lambda);
#pragma warning restore EF1001

        // Locating query.Execute methods
        var mExecute = query.GetType()
            .GetMethods()
            .SingleOrDefault(m => Equals(m.Name, nameof(query.Execute))
                && m.IsGenericMethod
                && m.GetGenericArguments().Length == batchSize)
            ?.MakeGenericMethod(pKeys.Select(p => p.Type).ToArray());
        if (mExecute == null)
            throw Errors.BatchSizeIsTooLarge();

        // Creating compiled query invoker
        var pAllKeys = Expression.Parameter(typeof(TKey[]));
        var eDbContext = Enumerable.Range(0, 1).Select(_ => (Expression)pDbContext);
        var eAllKeys = Enumerable.Range(0, batchSize).Select(i => Expression.ArrayIndex(pAllKeys, Expression.Constant(i)));
        var eExecuteCall = Expression.Call(Expression.Constant(query), mExecute, eDbContext.Concat(eAllKeys));
        return (Func<TDbContext, TKey[], IAsyncEnumerable<TDbEntity>>)Expression.Lambda(eExecuteCall, pDbContext, pAllKeys).Compile();
    }

    protected BatchProcessor<TKey, TDbEntity?> GetBatchProcessor(Symbol tenantId)
    {
        var batchProcessors = _batchProcessors;
        if (batchProcessors == null)
            throw Stl.Internal.Errors.AlreadyDisposed(GetType());

        return batchProcessors.GetOrAdd(tenantId,
            static (tenantId1, self) => self.CreateBatchProcessor(tenantId1), this);
    }

    protected virtual BatchProcessor<TKey, TDbEntity?> CreateBatchProcessor(Symbol tenantId)
    {
        var tenant = TenantRegistry.Get(tenantId);
        var batchProcessor = new BatchProcessor<TKey, TDbEntity?> {
            BatchSize = Settings.BatchSize,
            Implementation = (batch, cancellationToken) => ProcessBatch(tenant, batch, cancellationToken),
        };
        Settings.ConfigureBatchProcessor?.Invoke(batchProcessor);
        if (batchProcessor.BatchSize != Settings.BatchSize)
            throw Errors.BatchSizeCannotBeChanged();

        return batchProcessor;
    }

    protected virtual Activity? StartProcessBatchActivity(Tenant tenant, int batchSize)
    {
        var activitySource = GetType().GetActivitySource();
        var activity = activitySource
            .StartActivity(nameof(ProcessBatch))
            .AddTenantTags(tenant)?
            .AddTag("batchSize", batchSize.ToString(CultureInfo.InvariantCulture));
        return activity;
    }

    protected virtual async Task ProcessBatch(
        Tenant tenant,
        List<BatchProcessor<TKey, TDbEntity?>.Item> batch,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        using var activity = StartProcessBatchActivity(tenant, batch.Count);
        var (query, batchSize) = Queries.First(q => q.BatchSize >= batch.Count);
        for (var tryIndex = 0;; tryIndex++) {
            var dbContext = CreateDbContext(tenant);
            await using var _ = dbContext.ConfigureAwait(false);
            var keys = ArrayPool<TKey>.Shared.Rent(batchSize);
            try {
                var i = 0;
                foreach (var item in batch)
                    keys[i++] = item.Input;
                var lastKey = keys[i - 1];
                for (; i < batchSize; i++)
                    keys[i] = lastKey;

                var entities = new Dictionary<TKey, TDbEntity>();
                if (Settings.Timeout is { } timeout) {
                    using var cts = new CancellationTokenSource(timeout);
                    using var linkedCts = cancellationToken.LinkWith(cts.Token);
                    try {
                        var result = query.Invoke(dbContext, keys);
                        await foreach (var e in result.WithCancellation(cancellationToken).ConfigureAwait(false))
                            entities.Add(KeyExtractor.Invoke(e), e);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                        throw new TimeoutException();
                    }
                }
                else {
                    var result = query.Invoke(dbContext, keys);
                    await foreach (var e in result.WithCancellation(cancellationToken).ConfigureAwait(false))
                        entities.Add(KeyExtractor.Invoke(e), e);
                }
                Settings.PostProcessor.Invoke(entities);

                foreach (var item in batch) {
                    var entity = entities.GetValueOrDefault(item.Input);
                    // ReSharper disable once MethodSupportsCancellation
                    item.SetResult(entity);
                }
                return;
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested) {
                var isTransient = e is TimeoutException || TransientErrorDetector.IsTransient(e);
                if (!isTransient)
                    throw;

                var delayLogger = new RetryDelayLogger("process batch", Log);
                var delay = Settings.RetryDelayer.GetDelay(tryIndex + 1, delayLogger, cancellationToken);
                if (delay.IsLimitExceeded)
                    throw;

                if (!delay.Task.IsCompleted)
                    await delay.Task.ConfigureAwait(false);
            }
            finally {
                ArrayPool<TKey>.Shared.Return(keys);
            }
        }
    }
}
