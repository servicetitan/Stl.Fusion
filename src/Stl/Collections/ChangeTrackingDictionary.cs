using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Serialization;

namespace Stl.Collections
{
    public enum DictionaryEntryChangeType
    {
        Changed = 0,
        Added = 1,
        Removed = 2,
    }

    [JsonObject]
    public struct ChangeTrackingDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>
        where TKey : notnull
    {
        private static readonly ImmutableDictionary<TKey, TValue> EmptyDictionary =
            ImmutableDictionary<TKey, TValue>.Empty;
        private static readonly ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> EmptyChanges =
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>.Empty;

        public static readonly ChangeTrackingDictionary<TKey, TValue> Empty
            = new ChangeTrackingDictionary<TKey, TValue>(EmptyDictionary);

        public readonly ImmutableDictionary<TKey, TValue> Base;
        public readonly ImmutableDictionary<TKey, TValue> Dictionary;
        public readonly ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> Changes;

        [JsonIgnore]
        public int Count => Dictionary.Count;
        [JsonIgnore]
        public IEnumerable<TKey> Keys => Dictionary.Keys;
        [JsonIgnore]
        public IEnumerable<TValue> Values => Dictionary.Values;

        public TValue this[TKey key] => Dictionary[key];

        [JsonConstructor]
        public ChangeTrackingDictionary(
            ImmutableDictionary<TKey, TValue> @base,
            ImmutableDictionary<TKey, TValue>? dictionary = null,
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>? changes = null)
        {
            Base = @base;
            Dictionary = dictionary ?? @base;
            Changes = changes ?? EmptyChanges;
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
                ChangesWithUpdate(Base, Changes, key, value));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => AddRange(pairs);
        public ChangeTrackingDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            var newDictionary = Dictionary.AddRange(pairs);
            if (ReferenceEquals(newDictionary, Dictionary))
                return this;
            var _base = Base;
            var changes = Changes.SetItems(pairs.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(_base, p.Key), p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(_base, newDictionary, changes);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);
        public ChangeTrackingDictionary<TKey, TValue> Remove(TKey key)
            => new ChangeTrackingDictionary<TKey, TValue>(Base,
                Dictionary.Remove(key),
                ChangesWithRemoval(Base, Changes, key));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
            => RemoveRange(keys);
        public ChangeTrackingDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            var newDictionary = Dictionary.RemoveRange(keys);
            if (ReferenceEquals(newDictionary, Dictionary))
                return this;
            var _base = Base;
            var changes = keys.Aggregate(Changes, (c, key) => ChangesWithRemoval(_base, c, key));
            return new ChangeTrackingDictionary<TKey, TValue>(_base, newDictionary, changes);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
            => SetItem(key, value);
        public ChangeTrackingDictionary<TKey, TValue> SetItem(TKey key, TValue value)
            => new ChangeTrackingDictionary<TKey, TValue>(Base,
                Dictionary.SetItem(key, value),
                ChangesWithUpdate(Base, Changes, key, value));

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(
            IEnumerable<KeyValuePair<TKey, TValue>> items)
            => SetItems(items);
        public ChangeTrackingDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            var newDictionary = Dictionary.SetItems(items);
            if (ReferenceEquals(newDictionary, Dictionary))
                return this;
            var _base = Base;
            var changes = Changes.SetItems(items.Select(
                p => KeyValuePair.Create(p.Key, (GetChangedOrAdded(_base, p.Key), p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(_base, newDictionary, changes);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear() => Clear();
        public ChangeTrackingDictionary<TKey, TValue> Clear()
        {
            var changes = EmptyChanges
                .AddRange(Base.Select(
                    p => KeyValuePair.Create(p.Key, (DictionaryEntryChangeType.Removed, p.Value))));
            return new ChangeTrackingDictionary<TKey, TValue>(Base, EmptyDictionary, changes);
        }

        // Private / helpers

        private static DictionaryEntryChangeType GetChangedOrAdded(
            ImmutableDictionary<TKey, TValue> @base, TKey key) =>
            @base.ContainsKey(key)
                ? DictionaryEntryChangeType.Changed
                : DictionaryEntryChangeType.Added;

        private static ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> ChangesWithUpdate(
            ImmutableDictionary<TKey, TValue> @base,
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key, TValue value)
            => changes.SetItem(key, (GetChangedOrAdded(@base, key), value));

        private static ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> ChangesWithRemoval(
            ImmutableDictionary<TKey, TValue> @base,
            ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)> changes,
            TKey key)
            => @base.TryGetValue(key, out var v)
                ? changes.SetItem(key, (DictionaryEntryChangeType.Removed, v))
                : changes.Remove(key);
    }
}
