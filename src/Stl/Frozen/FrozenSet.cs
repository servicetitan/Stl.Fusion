using System;
using System.Collections;
using System.Collections.Generic;
using Stl.Collections;

namespace Stl.Frozen
{
    public interface IFrozenSet<T> : ISet<T>, IFrozenCollection<T> { }

    [Serializable]
    public class FrozenSet<T> : FrozenBase, IFrozenSet<T>
    {
        protected static readonly bool AreItemsFrozen =
            typeof(IFrozen).IsAssignableFrom(typeof(T));

        protected HashSet<T> Set { get; set; }
        public int Count => Set.Count;
        public bool IsReadOnly => IsFrozen;
        public IEqualityComparer<T> Comparer => Set.Comparer;

        public FrozenSet() : this(null!) { }
        public FrozenSet(IEqualityComparer<T> comparer)
            => Set = new HashSet<T>(comparer);
        public FrozenSet(int capacity, IEqualityComparer<T>? comparer = null)
            => Set = new HashSet<T>(capacity, comparer);

        // Read-only methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => Set.GetEnumerator();

        public bool Contains(T item) => Set.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => Set.CopyTo(array, arrayIndex);

        public bool IsProperSubsetOf(IEnumerable<T> other)
            => Set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other)
            => Set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other)
            => Set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other)
            => Set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other)
            => Set.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other)
            => Set.SetEquals(other);

        // Write methods

        void ICollection<T>.Add(T item) => Add(item);
        public bool Add(T item)
        {
            ThrowIfFrozen();
            return Set.Add(item);
        }

        public bool Remove(T item)
        {
            ThrowIfFrozen();
            return Set.Remove(item);
        }

        public void Clear()
        {
            ThrowIfFrozen();
            Set.Clear();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ThrowIfFrozen();
            Set.ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ThrowIfFrozen();
            Set.IntersectWith(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ThrowIfFrozen();
            Set.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            ThrowIfFrozen();
            Set.UnionWith(other);
        }

        // IFrozen-related

        public override void Freeze()
        {
            base.Freeze();
            if (!AreItemsFrozen)
                return;
            foreach (var item in Set)
                if (item is IFrozen f)
                    f.Freeze();
        }

        public override IFrozen CloneToUnfrozenUntyped(bool deep = false)
        {
            var clone = (FrozenSet<T>) base.CloneToUnfrozenUntyped(deep);
            if (!deep || !AreItemsFrozen) {
                clone.Set = new HashSet<T>(Comparer);
                clone.Set.AddRange(Set);
                return clone;
            }

            var set = new HashSet<T>(Count, Comparer);
            foreach (var item in Set) {
                if (item is IFrozen f)
                    set.Add((T) f.ToUnfrozen(deep));
                else
                    set.Add(item);
            }
            clone.Set = set;
            return clone;
        }
    }
}
