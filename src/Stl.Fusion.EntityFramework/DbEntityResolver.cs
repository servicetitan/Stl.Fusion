using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Stl.Internal;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public interface IDbEntityResolver<TKey, TDbEntity>
    where TKey : notnull
    where TDbEntity : class
{
    public Func<Expression, Expression> KeyExtractorExpressionBuilder { get; }
    public Func<TDbEntity, TKey> KeyExtractor { get; }

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
        public string? KeyPropertyName { get; init; }
        public Func<Expression, Expression>? KeyExtractorExpressionBuilder { get; init; }
        public Func<IQueryable<TDbEntity>, IQueryable<TDbEntity>> QueryTransformer { get; init; } = q => q;
        public Action<Dictionary<TKey, TDbEntity>> PostProcessor { get; init; } = _ => { };
        public Action<BatchProcessor<TKey, TDbEntity?>>? ConfigureBatchProcessor { get; init; }
    }

    protected static MethodInfo ContainsMethod { get; } = typeof(HashSet<TKey>).GetMethod(nameof(HashSet<TKey>.Contains))!;

    private ConcurrentDictionary<Symbol, BatchProcessor<TKey, TDbEntity?>>? _batchProcessors;

    protected Options Settings { get; }
    protected string KeyPropertyName { get; init; }

    public Func<Expression, Expression> KeyExtractorExpressionBuilder { get; init; }
    public Func<TDbEntity, TKey> KeyExtractor { get; init; }

    public DbEntityResolver(Options settings, IServiceProvider services) : base(services)
    {
        Settings = settings;
        if (settings.KeyPropertyName == null) {
            var dummyTenant = TenantRegistry.IsSingleTenant ? Tenant.Default : Tenant.Dummy;
            using var dbContext = CreateDbContext(dummyTenant);
            KeyPropertyName = dbContext.Model
                .FindEntityType(typeof(TDbEntity))!
                .FindPrimaryKey()!
                .Properties.Single().Name;
        }
        else
            KeyPropertyName = settings.KeyPropertyName;
        KeyExtractorExpressionBuilder = settings.KeyExtractorExpressionBuilder
            ?? (eEntity => Expression.PropertyOrField(eEntity, KeyPropertyName));
        var pEntity = Expression.Parameter(typeof(TDbEntity), "e");
        var eBody = KeyExtractorExpressionBuilder(pEntity);
        KeyExtractor = (Func<TDbEntity, TKey>) Expression.Lambda(eBody, pEntity).Compile();
        _batchProcessors = new();
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

    protected BatchProcessor<TKey, TDbEntity?> GetBatchProcessor(Symbol tenantId)
    {
        var batchProcessors = _batchProcessors;
        if (batchProcessors == null)
            throw Errors.AlreadyDisposed();
        return batchProcessors.GetOrAdd(tenantId,
            static (tenantId1, self) => self.CreateBatchProcessor(tenantId1), this);
    }

    protected virtual BatchProcessor<TKey, TDbEntity?> CreateBatchProcessor(Symbol tenantId)
    {
        var tenant = TenantRegistry.Get(tenantId);
        var batchProcessor = new BatchProcessor<TKey, TDbEntity?> {
            MaxBatchSize = 16,
            ConcurrencyLevel = 1,
            Implementation = (batch, cancellationToken) => ProcessBatch(tenant, batch, cancellationToken),
        };
        Settings.ConfigureBatchProcessor?.Invoke(batchProcessor);
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
        List<BatchItem<TKey, TDbEntity?>> batch,
        CancellationToken cancellationToken)
    {
        using var activity = StartProcessBatchActivity(tenant, batch.Count);
        var dbContext = CreateDbContext(tenant);
        await using var _ = dbContext.ConfigureAwait(false);

        var keys = new HashSet<TKey>();
        foreach (var item in batch) {
            if (!item.TryCancel(cancellationToken))
                keys.Add(item.Input);
        }
        var pEntity = Expression.Parameter(typeof(TDbEntity), "e");
        var eKey = KeyExtractorExpressionBuilder(pEntity);
        var eBody = Expression.Call(Expression.Constant(keys), ContainsMethod, eKey);
        var eLambda = (Expression<Func<TDbEntity, bool>>) Expression.Lambda(eBody, pEntity);
        var query = Settings.QueryTransformer(dbContext.Set<TDbEntity>().Where(eLambda));
        var entities = await query
            .ToDictionaryAsync(KeyExtractor, cancellationToken)
            .ConfigureAwait(false);
        Settings.PostProcessor(entities);

        foreach (var item in batch) {
            var entity = entities.GetValueOrDefault(item.Input);
            item.SetResult(Result.Value(entity)!, cancellationToken);
        }
    }
}
