using System;

namespace Stl.Tests.Fusion
{
    public static class ChangeValidators<TKey, TEntity>
        where TKey : notnull
        where TEntity : IHasKey<TKey>
    {
        public static readonly Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> None = 
            (n, o) => null;
        public static readonly Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> Add = 
            (n, o) => o.Entity.HasValue ? "Entity already exists." : null;
        public static readonly Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> Update = 
            (n, o) => !o.Entity.HasValue ? "Entity doesn't exist." : null; 
        public static readonly Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> Remove = Update; 

        public static Func<MaybeEntity<TKey, TEntity>, MaybeEntity<TKey, TEntity>, string?> Get(ChangeKind changeKind) 
            => changeKind switch {
                ChangeKind.Add => Add,
                ChangeKind.Remove => Remove,
                ChangeKind.Update => Update,
                _ => throw new ArgumentOutOfRangeException(nameof(changeKind), changeKind, null)
            };
    }
}
