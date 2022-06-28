using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Stl.OS;

namespace Stl.Fusion.EntityFramework;

public interface IDbEntityResolver<TKey, TDbEntity>
    where TKey : notnull
    where TDbEntity : class
{
    Task<TDbEntity?> Get(TKey key, CancellationToken cancellationToken = default);
    Task<Dictionary<TKey, TDbEntity>> GetMany(IEnumerable<TKey> keys, CancellationToken cancellationToken = default);
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
    IDisposable
    where TDbContext : DbContext
    where TKey : notnull
    where TDbEntity : class
{
    public record Options
    {
        public Func<DbEntityResolver<TDbContext, TKey, TDbEntity>, BatchProcessor<TKey, TDbEntity>>? BatchProcessorFactory { get; init; }
        public string? KeyPropertyName { get; init; }
        public Func<Expression, Expression>? KeyExtractorExpressionBuilder { get; init; }
        public Func<IQueryable<TDbEntity>, IQueryable<TDbEntity>> QueryTransformer { get; init; } = q => q;
        public Action<Dictionary<TKey, TDbEntity>> PostProcessor { get; init; } = _ => { };
    }

    protected static MethodInfo ContainsMethod { get; } = typeof(HashSet<TKey>).GetMethod(nameof(HashSet<TKey>.Contains))!;

    private readonly Lazy<BatchProcessor<TKey, TDbEntity>> _batchProcessorLazy;

    protected Options Settings { get; }
    protected BatchProcessor<TKey, TDbEntity> BatchProcessor => _batchProcessorLazy.Value;
    protected string KeyPropertyName { get; init; }
    protected Func<TDbEntity, TKey> KeyExtractor { get; init; }
    protected Func<Expression, Expression> KeyExtractorExpressionBuilder { get; init; }

    public DbEntityResolver(Options settings, IServiceProvider services) : base(services)
    {
        Settings = settings;
        var batchProcessorFactory = settings.BatchProcessorFactory ??
            (self => new BatchProcessor<TKey, TDbEntity> {
                MaxBatchSize = 16,
                ConcurrencyLevel = Math.Min(HardwareInfo.ProcessorCount, 4),
                BatchingDelayTaskFactory = cancellationToken => Task.Delay(1, cancellationToken),
                Implementation = self.ProcessBatch,
            });
        _batchProcessorLazy = new Lazy<BatchProcessor<TKey, TDbEntity>>(
            () => batchProcessorFactory(this));

        if (settings.KeyPropertyName == null) {
            using var dbContext = CreateDbContext();
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
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (_batchProcessorLazy.IsValueCreated)
            BatchProcessor.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual async Task<TDbEntity?> Get(TKey key, CancellationToken cancellationToken = default)
        => await BatchProcessor.Process(key, cancellationToken).ConfigureAwait(false);

    public virtual async Task<Dictionary<TKey, TDbEntity>> GetMany(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Distinct().Select(key => Get(key, cancellationToken)).ToArray();
        var entities = await Task.WhenAll(tasks).ConfigureAwait(false);
        var result = new Dictionary<TKey, TDbEntity>();
        foreach (var entity in entities)
            if (entity != null!)
                result.Add(KeyExtractor(entity), entity);
        return result;
    }

    // Protected methods

    protected virtual async Task ProcessBatch(List<BatchItem<TKey, TDbEntity>> batch, CancellationToken cancellationToken)
    {
        // using var activity = ActivitySource.StartActivity(ProcessBatchOperationName);
        // activity?.AddTag("batchSize", batch.Count.ToString(CultureInfo.InvariantCulture));

        var dbContext = CreateDbContext();
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
            entities.TryGetValue(item.Input, out var entity);
            item.SetResult(Result.Value(entity)!, cancellationToken);
        }
    }
}
