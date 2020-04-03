using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Locking;

namespace Stl.Tests.Purifier
{
    public class TestRepository<TKey, TEntity> : IRepository<TKey, TEntity>
        where TKey : notnull
        where TEntity : IHasKey<TKey>
    {
        protected readonly ConcurrentDictionary<TKey, TEntity> Entities = new ConcurrentDictionary<TKey, TEntity>(); 
        protected readonly AsyncLockSet<TKey> LockSet = new AsyncLockSet<TKey>(ReentryMode.UncheckedDeadlock);

        public ValueTask<Option<TEntity>> TryGetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (Entities.TryGetValue(key, out var value))
                return ValueTaskEx.New(Option<TEntity>.Some(value));
            return ValueTaskEx.New(Option<TEntity>.None); 
        }

        public async ValueTask<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var vOption = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
            if (!vOption.IsSome(out var v))
                throw new KeyNotFoundException();
            return v;
        }

        public async Task ChangeAsync(ChangeKind changeKind, MaybeEntity<TKey, TEntity> maybeEntity, 
            Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> changeValidator,
            CancellationToken cancellationToken = default)
        {
            var key = maybeEntity.Key;
            using var _ = await LockSet.LockAsync(key, cancellationToken).ConfigureAwait(false);

            var hasValidator = changeValidator != ChangeValidators<TKey, TEntity>.None;
            if (hasValidator) {
                var oldEntity = await TryGetAsync(key, cancellationToken).ConfigureAwait(false);
                var result = changeValidator.Invoke(maybeEntity, new MaybeEntity<TKey, TEntity>(key, oldEntity));
                if (!string.IsNullOrEmpty(result))
                    throw new ValidationException(result); 
            }

            await DelayAsync(changeKind, key).ConfigureAwait(false);

            switch (changeKind) {
            case ChangeKind.Add:
            case ChangeKind.Update:
                Entities[key] = maybeEntity.Entity.Value;
                break;
            case ChangeKind.Remove:
                Entities.Remove(key, out var v);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changeKind), changeKind, null);
            }
        }

        protected virtual ValueTask DelayAsync(ChangeKind changeKind, TKey key) => ValueTaskEx.CompletedTask;
    }
}
