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

    public class ChangeTrackingDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>
        where TKey : notnull
    {
        public ImmutableDictionary<TKey, TValue> Base { get; }
        public ImmutableDictionary<TKey, TValue> Dictionary { get; }
        public ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> Changes { get; }

        public int Count => Dictionary.Count;
        public TValue this[TKey key] => Dictionary[key];
        public IEnumerable<TKey> Keys => Dictionary.Keys;
        public IEnumerable<TValue> Values => Dictionary.Values;
        
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
        public bool TryGetKey(TKey equalKey, out TKey actualKey) => Dictionary.TryGetKey(equalKey, out actualKey);
        public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value) 
            => Add(key, value);
        public ChangeTrackingDictionary<TKey, TValue> Add(TKey key, TValue value)
            => new ChangeTrackingDictionary<TKey, TValue>(Base, 
                Dictionary.Add(key, value), 
                ChangesPlusUpdate(Changes, key, value));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => AddRange(pairs);
        public ChangeTrackingDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            var newDictionary = Dictionary.AddRange(pairs);
            var changes = Changes.SetItems(pairs.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(p.Key), p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(Base, newDictionary, changes);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key) => Remove(key); 
        public ChangeTrackingDictionary<TKey, TValue> Remove(TKey key) 
            => new ChangeTrackingDictionary<TKey, TValue>(Base, 
                Dictionary.Remove(key), 
                ChangesPlusRemoval(Changes, key));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys) 
            => RemoveRange(keys);
        public ChangeTrackingDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            var newDictionary = Dictionary.RemoveRange(keys);
            var changes = keys.Aggregate(Changes, ChangesPlusRemoval);
            return new ChangeTrackingDictionary<TKey, TValue>(Base, newDictionary, changes);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value) 
            => SetItem(key, value);
        public ChangeTrackingDictionary<TKey, TValue> SetItem(TKey key, TValue value)
            => new ChangeTrackingDictionary<TKey, TValue>(Base, 
                Dictionary.SetItem(key, value), 
                ChangesPlusUpdate(Changes, key, value));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(
            IEnumerable<KeyValuePair<TKey, TValue>> items)
            => SetItems(items);
        public ChangeTrackingDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            var newDictionary = Dictionary.SetItems(items);
            var changes = Changes.SetItems(items.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(p.Key), p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(Base, newDictionary, changes);
        }
        
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear() => Clear(); 
        public ChangeTrackingDictionary<TKey, TValue> Clear()
        {
            var changes = ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>
                .Empty
                .AddRange(Base.Select(
                    p => KeyValuePair.Create(p.Key, (DictionaryEntryChangeType.Removed, p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(Base, ImmutableDictionary<TKey, TValue>.Empty, changes);
        }

        // Private / helpers

        private DictionaryEntryChangeType GetChangedOrAdded(TKey key) =>
            Base.ContainsKey(key) 
                ? DictionaryEntryChangeType.Changed 
                : DictionaryEntryChangeType.Added;

        private ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> ChangesPlusUpdate(
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key, TValue value) 
            => changes.SetItem(key, (GetChangedOrAdded(key), value));

        private ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> ChangesPlusRemoval(
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key) 
            => Base.TryGetValue(key, out var v)
                ? Changes.SetItem(key, (DictionaryEntryChangeType.Removed, v))
                : Changes.Remove(key);
    }
}