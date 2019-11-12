using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Stl.Collections
{
    public enum DictionaryEntryChangeType
    {
        Changed = 0,
        Added = 1,
        Removed = 2,
    }

    public class ChangeTrackingDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        public ImmutableDictionary<TKey, TValue> Base { get; }
        public ImmutableDictionary<TKey, TValue> Dictionary { get; private set;  }
        public ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> Changes { get; private set; }

        public bool IsReadOnly => false;
        public int Count => Dictionary.Count;
        public ICollection<TKey> Keys => throw new NotSupportedException();
        public ICollection<TValue> Values => throw new NotSupportedException();

        public TValue this[TKey key] {
            get => Dictionary[key];
            set {
                if (!DictionaryUpdate(Dictionary.SetItem(key, value))) 
                    return;
                AddUpdate(Changes, key, value);
            }
        }

        public ChangeTrackingDictionary(
            ImmutableDictionary<TKey, TValue> @base, 
            ImmutableDictionary<TKey, TValue> dictionary = null, 
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>? changes = null)
        {
            Base = @base;
            Dictionary = dictionary ?? @base;
            Changes = changes ?? ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>.Empty;
        }

        IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();

        public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> pair) => Dictionary.Contains(pair);
        public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);

        public void Clear()
        {
            if (!DictionaryUpdate(ImmutableDictionary<TKey, TValue>.Empty)) 
                return;
            Changes = ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>
                .Empty
                .AddRange(Base.Select(
                    p => KeyValuePair.Create(p.Key, (DictionaryEntryChangeType.Removed, p.Value))));
        }

        public void Add(KeyValuePair<TKey, TValue> item) 
            => Add(item.Key, item.Value);

        public void Add(TKey key, TValue value)
        {
            if (!DictionaryUpdate(Dictionary.Add(key, value))) 
                return;
            Changes = AddUpdate(Changes, key, value);
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            if (!DictionaryUpdate(Dictionary.AddRange(items))) 
                return;
            Changes = Changes.SetItems(items.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(p.Key), p.Value))));
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Dictionary.TryGetValue(item.Key, out var v))
                return false;
            if (!EqualityComparer<TValue>.Default.Equals(v, item.Value))
                return false;
            Dictionary = Dictionary.Remove(item.Key);
            Changes = AddRemoval(Changes, item.Key);
            return true;
        }

        public bool Remove(TKey key)
        {
            if (!DictionaryUpdate(Dictionary.Remove(key))) 
                return false;
            Changes = AddRemoval(Changes, key);
            return true;
        }

        public void RemoveRange(IEnumerable<TKey> keys)
        {
            if (!DictionaryUpdate(Dictionary.RemoveRange(keys))) 
                return;
            Changes = keys.Aggregate(Changes, AddRemoval);
        }

        public void SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            if (!DictionaryUpdate(Dictionary.SetItems(items))) 
                return;
            Changes = Changes.SetItems(items.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(p.Key), p.Value))));
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) 
            => Dictionary.ToArray().CopyTo(array, arrayIndex);

        // Private / helpers

        private bool DictionaryUpdate(ImmutableDictionary<TKey, TValue> newDictionary)
        {
            if (ReferenceEquals(newDictionary, Dictionary))
                return false;
            Dictionary = newDictionary;
            return true;
        }

        private DictionaryEntryChangeType GetChangedOrAdded(TKey key) =>
            Base.ContainsKey(key) 
                ? DictionaryEntryChangeType.Changed 
                : DictionaryEntryChangeType.Added;

        private ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> AddUpdate(
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key, TValue value) 
            => changes.SetItem(key, (GetChangedOrAdded(key), value));

        private ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> AddRemoval(
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key) 
            => Base.TryGetValue(key, out var v)
                ? Changes.SetItem(key, (DictionaryEntryChangeType.Removed, v))
                : Changes.Remove(key);
    }
}