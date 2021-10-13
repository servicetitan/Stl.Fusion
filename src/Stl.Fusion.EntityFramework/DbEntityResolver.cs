using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stl.Async;
using Stl.Fusion.EntityFramework.Internal;
using Stl.OS;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbEntityResolver<TKey, TDbEntity>
        where TKey : notnull
        where TDbEntity : class
    {
        Task<TDbEntity> Get(TKey key, CancellationToken cancellationToken = default);
        Task<TDbEntity?> TryGet(TKey key, CancellationToken cancellationToken = default);
        Task<Dictionary<TKey, TDbEntity>> GetMany(IEnumerable<TKey> keys, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// This type queues (when needed) & batches calls to <see cref="TryGet"/> with
    /// <see cref="AsyncBatchProcessor{TIn,TOut}"/> to reduce the rate of underlying DB queries.
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
        public class Options
        {
            public Func<DbEntityResolver<TDbContext, TKey, TDbEntity>, AsyncBatchProcessor<TKey, TDbEntity>>? BatchProcessorFactory { get; set; }
            public string? KeyPropertyName { get; set; }
            public Func<Expression, Expression>? KeyExtractorExpressionBuilder { get; set; }
            public Func<IQueryable<TDbEntity>, IQueryable<TDbEntity>>? QueryTransformer { get; set; }
            public Action<Dictionary<TKey, TDbEntity>>? PostProcessor { get; set; }
        }

        protected static MethodInfo ContainsMethod { get; } = typeof(HashSet<TKey>).GetMethod(nameof(HashSet<TKey>.Contains))!;

        private readonly Lazy<AsyncBatchProcessor<TKey, TDbEntity>> _batchProcessorLazy;
        protected Func<DbEntityResolver<TDbContext, TKey, TDbEntity>, AsyncBatchProcessor<TKey, TDbEntity>> BatchProcessorFactory { get; set; }
        protected AsyncBatchProcessor<TKey, TDbEntity> BatchProcessor => _batchProcessorLazy.Value;
        protected Func<Expression, Expression> KeyExtractorExpressionBuilder { get; set; }
        protected Func<TDbEntity, TKey> KeyExtractor { get; set; }
        protected Func<IQueryable<TDbEntity>, IQueryable<TDbEntity>> QueryTransformer { get; set; }
        protected Action<Dictionary<TKey, TDbEntity>> PostProcessor { get; set; }

        public DbEntityResolver(IServiceProvider services) : this(null, services) { }
        public DbEntityResolver(Options? options, IServiceProvider services) : base(services)
        {
            options ??= new();
            BatchProcessorFactory = options.BatchProcessorFactory ??
                (self => new AsyncBatchProcessor<TKey, TDbEntity> {
                    MaxBatchSize = 16,
                    ConcurrencyLevel = Math.Min(HardwareInfo.ProcessorCount, 4),
                    BatchingDelayTaskFactory = cancellationToken => Task.Delay(1, cancellationToken),
                    BatchProcessor = self.ProcessBatch,
                });
            _batchProcessorLazy = new Lazy<AsyncBatchProcessor<TKey, TDbEntity>>(
                () => BatchProcessorFactory.Invoke(this));

            using var dbContext = CreateDbContext();
            var keyPropertyName = options.KeyPropertyName
                ?? dbContext.Model.FindEntityType(typeof(TDbEntity)).FindPrimaryKey().Properties.Single().Name;
            KeyExtractorExpressionBuilder = options.KeyExtractorExpressionBuilder
                ?? (eEntity => Expression.PropertyOrField(eEntity, keyPropertyName));
            var pEntity = Expression.Parameter(typeof(TDbEntity), "e");
            var eBody = KeyExtractorExpressionBuilder.Invoke(pEntity);
            KeyExtractor = (Func<TDbEntity, TKey>) Expression.Lambda(eBody, pEntity).Compile();

            QueryTransformer = options.QueryTransformer ?? (q => q);
            PostProcessor = options.PostProcessor ?? (_ => {});
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_batchProcessorLazy.IsValueCreated)
                BatchProcessor.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual async Task<TDbEntity> Get(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await TryGet(key, cancellationToken).ConfigureAwait(false);
            return entity ?? throw Errors.EntityNotFound<TDbEntity>();
        }

        public virtual async Task<TDbEntity?> TryGet(TKey key, CancellationToken cancellationToken = default)
            => await BatchProcessor.Process(key, cancellationToken).ConfigureAwait(false);

        public virtual async Task<Dictionary<TKey, TDbEntity>> GetMany(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
        {
            var tasks = keys.Distinct().Select(key => TryGet(key, cancellationToken)).ToArray();
            var entities = await Task.WhenAll(tasks).ConfigureAwait(false);
            var result = new Dictionary<TKey, TDbEntity>();
            foreach (var entity in entities)
                if (entity != null!)
                    result.Add(KeyExtractor.Invoke(entity), entity);
            return result;
        }

        // Protected methods

        protected virtual async Task ProcessBatch(List<BatchItem<TKey, TDbEntity>> batch, CancellationToken cancellationToken)
        {
            await using var dbContext = CreateDbContext();
            var keys = new HashSet<TKey>();
            foreach (var item in batch) {
                if (!item.TryCancel(cancellationToken))
                    keys.Add(item.Input);
            }
            var pEntity = Expression.Parameter(typeof(TDbEntity), "e");
            var eKey = KeyExtractorExpressionBuilder.Invoke(pEntity);
            var eBody = Expression.Call(Expression.Constant(keys), ContainsMethod, eKey);
            var eLambda = (Expression<Func<TDbEntity, bool>>) Expression.Lambda(eBody, pEntity);
            var query = QueryTransformer.Invoke(dbContext.Set<TDbEntity>().Where(eLambda));
            var entities = await query
                .ToDictionaryAsync(KeyExtractor, cancellationToken)
                .ConfigureAwait(false);
            PostProcessor.Invoke(entities);

            foreach (var item in batch) {
                entities.TryGetValue(item.Input, out var entity);
                item.SetResult(Result.Value(entity)!, CancellationToken.None);
            }
        }
    }
}
