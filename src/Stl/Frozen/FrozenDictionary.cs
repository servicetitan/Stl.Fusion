using System;
using System.Collections;
using System.Collections.Generic;

namespace Stl.Frozen
{
    public interface IFrozenDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        IFrozenCollection<KeyValuePair<TKey, TValue>>
    { }

    [Serializable]
    public class FrozenDictionary<TKey, TValue> : FrozenBase, IFrozenDictionary<TKey, TValue>
    {
        protected static readonly bool AreValuesFrozen =
            typeof(IFrozen).IsAssignableFrom(typeof(TValue));

        protected Dictionary<TKey, TValue> Dictionary { get; set; }
        protected ICollection<KeyValuePair<TKey, TValue>> DictionaryAsCollection => Dictionary;
        public int Count => Dictionary.Count;
        public bool IsReadOnly => IsFrozen;
        public IEqualityComparer<TKey> Comparer => Dictionary.Comparer;
        public ICollection<TKey> Keys => Dictionary.Keys;
        public ICollection<TValue> Values => Dictionary.Values;

        public TValue this[TKey key] {
            get => Dictionary[key];
            set {
                ThrowIfFrozen();
                Dictionary[key] = value;
            }
        }

        public FrozenDictionary() : this(0) { }
        public FrozenDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }
        public FrozenDictionary(int capacity, IEqualityComparer<TKey>? comparer = null)
            => Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        public FrozenDictionary(Dictionary<TKey, TValue> dictionary)
            => Dictionary = dictionary;
        public FrozenDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : this(source, null) { }
        public FrozenDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer)
            : this((source as ICollection<KeyValuePair<TKey, TValue>>)?.Count ?? 0, comparer)
        {
            foreach (var (key, value) in source)
                Add(key, value);
        }

        // Write methods

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ThrowIfFrozen();
            DictionaryAsCollection.Add(item);
        }

        public void Add(TKey key, TValue value)
        {
            ThrowIfFrozen();
            Dictionary.Add(key, value);
        }

        public void Clear()
        {
            ThrowIfFrozen();
            Dictionary.Clear();
        }

        public bool Remove(TKey key)
        {
            ThrowIfFrozen();
            return Dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            ThrowIfFrozen();
            return DictionaryAsCollection.Remove(item);
        }

        // Read-only methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => Dictionary.GetEnumerator();
        public bool TryGetValue(TKey key, out TValue value)
            => Dictionary.TryGetValue(key, out value);
        public bool ContainsKey(TKey key)
            => Dictionary.ContainsKey(key);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => DictionaryAsCollection.Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => DictionaryAsCollection.CopyTo(array, arrayIndex);

        // IFrozen-related

        public override void Freeze()
        {
            base.Freeze();
            if (!AreValuesFrozen)
                return;
            foreach (var (key, value) in Dictionary)
                if (value is IFrozen f)
                    f.Freeze();
        }

        public override IFrozen CloneToUnfrozenUntyped(bool deep = false)
        {
            var clone = (FrozenDictionary<TKey, TValue>) base.CloneToUnfrozenUntyped(deep);
            if (!deep || !AreValuesFrozen) {
                clone.Dictionary = new Dictionary<TKey, TValue>(Comparer);
                return clone;
            }

            var d = new Dictionary<TKey, TValue>(Count, Comparer);
            foreach (var (key, value) in Dictionary) {
                if (value is IFrozen f)
                    d.Add(key, (TValue) f.ToUnfrozen(deep));
                else
                    d.Add(key, value);
            }
            clone.Dictionary = d;
            return clone;
        }
    }
}
