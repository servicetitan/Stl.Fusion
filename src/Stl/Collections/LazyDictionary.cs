using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stl.Collections
{
    public class LazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        protected Dictionary<TKey, TValue>? _dictionary;

        public Dictionary<TKey, TValue> Dictionary =>
            _dictionary ??= new Dictionary<TKey, TValue>();

        public int Count => _dictionary?.Count ?? 0;
        public bool IsReadOnly => false;

        public ICollection<TKey> Keys =>
            _dictionary?.Keys ?? (ICollection<TKey>) Array.Empty<TKey>();
        public ICollection<TValue> Values =>
            _dictionary?.Values ?? (ICollection<TValue>) Array.Empty<TValue>();

        public TValue this[TKey key] {
            get => _dictionary != null ? _dictionary[key] : throw new KeyNotFoundException();
            set => Dictionary[key] = value;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            _dictionary?.GetEnumerator()
            ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();

        public void Clear() => _dictionary = null;

        public void Add(TKey key, TValue value) => Dictionary.Add(key, value);
        public bool ContainsKey(TKey key) => _dictionary?.ContainsKey(key) ?? false;
        public bool Remove(TKey key) => _dictionary?.Remove(key) ?? false;
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_dictionary == null) {
                value = default!;
                return false;
            }
            return _dictionary.TryGetValue(key, out value!);
        }

        // ICollection

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
            Dictionary.Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
            _dictionary?.Contains(item) ?? false;

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (_dictionary is ICollection<KeyValuePair<TKey, TValue>> c)
                c.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_dictionary is ICollection<KeyValuePair<TKey, TValue>> c)
                return c.Remove(item);
            return false;
        }
    }
}
