using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Tests.Fusion
{
    public readonly struct MaybeEntity<TKey, TEntity> : IEquatable<MaybeEntity<TKey, TEntity>> 
        where TKey : notnull
        where TEntity : IHasKey<TKey>
    {
        public TKey Key { get; }
        public Option<TEntity> Entity { get; }

        // Construction & conversion

        public MaybeEntity(TKey key) : this() => Key = key;
        
        public MaybeEntity(TEntity entity) : this()
        {
            Entity = entity;
            Key = entity.Key;
        }

        public MaybeEntity(TKey key, Option<TEntity> entity) : this()
        {
            Key = key;
            Entity = entity;
            if (entity.IsSome(out var e)) {
                if (!EqualityComparer<TKey>.Default.Equals(key, e.Key))
                    throw new InvalidOperationException("key != entity.Value.Key.");
            }
        }

        public void Deconstruct(out TKey key, out Option<TEntity> entity)
        {
            key = Key;
            entity = Entity;
        }

        public static implicit operator MaybeEntity<TKey, TEntity>((TKey Key, Option<TEntity> Entity) source) 
            => new MaybeEntity<TKey, TEntity>(source.Key, source.Entity);

        public override string ToString() 
            => $"{GetType().Name}({(IsEntity(out var e) ? (object?) e : Key)})";

        // Useful members

        public bool IsEntity([MaybeNullWhen(false)] out TEntity value) => Entity.IsSome(out value!);

        // Equality members

        public bool Equals(MaybeEntity<TKey, TEntity> other) 
            => EqualityComparer<TKey>.Default.Equals(Key, other.Key) && Entity.Equals(other.Entity);
        public override bool Equals(object? obj) 
            => obj is MaybeEntity<TKey, TEntity> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Entity);
        public static bool operator ==(MaybeEntity<TKey, TEntity> left, MaybeEntity<TKey, TEntity> right) => left.Equals(right);
        public static bool operator !=(MaybeEntity<TKey, TEntity> left, MaybeEntity<TKey, TEntity> right) => !left.Equals(right);
    }
}
