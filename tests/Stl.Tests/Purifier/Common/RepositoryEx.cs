using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Tests.Purifier
{
    public static class RepositoryEx
    {
        // ChangeAsync overloads

        public static Task ChangeAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            ChangeKind changeKind,
            MaybeEntity<TKey, TEntity> maybeEntity, 
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(changeKind, maybeEntity, ChangeValidators<TKey, TEntity>.Get(changeKind), cancellationToken);

        // UpdateAsync overloads

        public static Task UpdateAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TEntity entity, 
            Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> changeValidator,
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Update, new MaybeEntity<TKey, TEntity>(entity), changeValidator, cancellationToken);

        public static Task UpdateAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            MaybeEntity<TKey, TEntity> maybeEntity, 
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Update, maybeEntity, ChangeValidators<TKey, TEntity>.Update, cancellationToken);

        // AddAsync overloads

        public static Task AddAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TEntity entity, 
            Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> changeValidator,
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Add, new MaybeEntity<TKey, TEntity>(entity), changeValidator, cancellationToken);

        public static Task AddAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TEntity entity,
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Add, new MaybeEntity<TKey, TEntity>(entity), ChangeValidators<TKey, TEntity>.Add, cancellationToken);

        // RemoveAsync overloads

        public static Task RemoveAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TEntity entity, 
            Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> changeValidator,
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Remove, new MaybeEntity<TKey, TEntity>(entity), changeValidator, cancellationToken);

        public static Task RemoveAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TEntity entity, 
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Remove, new MaybeEntity<TKey, TEntity>(entity), ChangeValidators<TKey, TEntity>.Remove, cancellationToken);

        public static Task RemoveAsync<TKey, TEntity>(this IRepository<TKey, TEntity> repository,
            TKey key, 
            CancellationToken cancellationToken = default)
            where TKey : notnull
            where TEntity : IHasKey<TKey>
            => repository.ChangeAsync(ChangeKind.Remove, new MaybeEntity<TKey, TEntity>(key), ChangeValidators<TKey, TEntity>.None, cancellationToken);
    }
}
