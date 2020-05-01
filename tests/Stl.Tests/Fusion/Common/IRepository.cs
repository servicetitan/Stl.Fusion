using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Caching;

namespace Stl.Tests.Fusion
{
    public interface IHasKey<out TKey>
        where TKey : notnull
    {
        TKey Key { get; }
    }

    public interface IRepository<TKey, TEntity> : IAsyncKeyResolver<TKey, TEntity>
        where TKey : notnull
        where TEntity : IHasKey<TKey>
    {
        Task ChangeAsync(ChangeKind changeKind, 
            MaybeEntity<TKey, TEntity> maybeEntity, 
            Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> changeValidator, 
            CancellationToken cancellationToken = default);
    }

    public interface IQueryableRepository<TKey, TEntity> : IAsyncKeyResolver<TKey, TEntity>
        where TKey : notnull
        where TEntity : IHasKey<TKey>
    {
        IQueryable<TEntity> All { get; }
    }
}
