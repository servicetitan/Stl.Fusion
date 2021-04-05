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
    /// <summary>
    /// This type queues (when needed) & batches calls to <see cref="TryGet"/> with
    /// <see cref="AsyncBatchProcessor{TIn,TOut}"/> to reduce the rate of underlying DB queries.
    /// </summary>
    /// <typeparam name="TDbContext">The type of <see cref="DbContext"/>.</typeparam>
    /// <typeparam name="TKey">The type of entity key.</typeparam>
    /// <typeparam name="TEntity">The type of entity to pipeline batch for.</typeparam>
    public class DbEntityResolver<TDbContext, TKey, TEntity> : DbServiceBase<TDbContext>, IDisposable
        where TDbContext : DbContext
        where TKey : notnull
        where TEntity : class
    {
        public class Options
        {
            public Func<DbEntityResolver<TDbContext, TKey, TEntity>, AsyncBatchProcessor<TKey, TEntity>>? BatchProcessorFactory { get; set; }
            public Func<Expression, Expression>? KeyExtractorExpressionBuilder { get; set; }
            public Func<IQueryable<TEntity>, IQueryable<TEntity>>? QueryTransformer { get; set; }
            public Action<Dictionary<TKey, TEntity>>? PostProcessor { get; set; }
        }

        protected static MethodInfo ContainsMethod { get; } = typeof(HashSet<TKey>).GetMethod(nameof(HashSet<TKey>.Contains))!;

        private readonly Lazy<AsyncBatchProcessor<TKey, TEntity>> _batchProcessorLazy;
        protected Func<DbEntityResolver<TDbContext, TKey, TEntity>, AsyncBatchProcessor<TKey, TEntity>> BatchProcessorFactory { get; set; }
        protected AsyncBatchProcessor<TKey, TEntity> BatchProcessor => _batchProcessorLazy.Value;
        protected Func<Expression, Expression> KeyExtractorExpressionBuilder { get; set; }
        protected Func<TEntity, TKey> KeyExtractor { get; set; }
        protected Func<IQueryable<TEntity>, IQueryable<TEntity>> QueryTransformer { get; set; }
        protected Action<Dictionary<TKey, TEntity>> PostProcessor { get; set; }

        public DbEntityResolver(IServiceProvider services) : this(null, services) { }
        public DbEntityResolver(Options? options, IServiceProvider services) : base(services)
        {
            options ??= new();
            BatchProcessorFactory = options.BatchProcessorFactory ??
                (self => new AsyncBatchProcessor<TKey, TEntity> {
                    MaxBatchSize = 16,
                    ConcurrencyLevel = Math.Min(HardwareInfo.ProcessorCount, 4),
                    BatchingDelayTaskFactory = cancellationToken => Task.Delay(1, cancellationToken),
                    BatchProcessor = self.ProcessBatch,
                });
            _batchProcessorLazy = new Lazy<AsyncBatchProcessor<TKey, TEntity>>(
                () => BatchProcessorFactory.Invoke(this));

            using var dbContext = CreateDbContext();
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
            var key = entityType.FindPrimaryKey();

            KeyExtractorExpressionBuilder = options.KeyExtractorExpressionBuilder ??
                (eEntity => Expression.PropertyOrField(eEntity, key.Properties.Single().Name));
            var pEntity = Expression.Parameter(typeof(TEntity), "e");
            var eBody = KeyExtractorExpressionBuilder.Invoke(pEntity);
            KeyExtractor = (Func<TEntity, TKey>) Expression.Lambda(eBody, pEntity).Compile();

            QueryTransformer = options.QueryTransformer ?? (q => q);
            PostProcessor = options.PostProcessor ?? (_ => {});
        }

        void IDisposable.Dispose()
        {
            if (_batchProcessorLazy.IsValueCreated)
                BatchProcessor.Dispose();
        }

        public async Task<TEntity> Get(TKey key, CancellationToken cancellationToken = default)
        {
            var entity = await TryGet(key, cancellationToken).ConfigureAwait(false);
            return entity ?? throw Errors.EntityNotFound<TEntity>();
        }

        public async Task<TEntity?> TryGet(TKey key, CancellationToken cancellationToken = default)
            => await BatchProcessor.Process(key, cancellationToken).ConfigureAwait(false);

        public async Task<Dictionary<TKey, TEntity>> GetMany(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
        {
            var tasks = keys.Distinct().Select(key => TryGet(key, cancellationToken)).ToArray();
            var entities = await Task.WhenAll(tasks).ConfigureAwait(false);
            var result = new Dictionary<TKey, TEntity>();
            foreach (var entity in entities)
                if (entity != null!)
                    result.Add(KeyExtractor.Invoke(entity), entity);
            return result;
        }

        // Protected methods

        protected virtual async Task ProcessBatch(List<BatchItem<TKey, TEntity>> batch, CancellationToken cancellationToken)
        {
            await using var dbContext = CreateDbContext();
            var keys = new HashSet<TKey>();
            foreach (var item in batch) {
                if (!item.TryCancel(cancellationToken))
                    keys.Add(item.Input);
            }
            var pEntity = Expression.Parameter(typeof(TEntity), "e");
            var eKey = KeyExtractorExpressionBuilder.Invoke(pEntity);
            var eBody = Expression.Call(Expression.Constant(keys), ContainsMethod, eKey);
            var eLambda = (Expression<Func<TEntity, bool>>) Expression.Lambda(eBody, pEntity);
            var query = QueryTransformer.Invoke(dbContext.Set<TEntity>().Where(eLambda));
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
