using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stl.ImmutableModel
{
    // It's intentional this type supports IEnumerable<(TKey Key, object? Value)> rather than
    // IEnumerable<KeyValuePair<TKey Key, object? Value>> - if the second one is supported,
    // implementing it in types that also implement IEnumerable<KeyValuePair<TKey Key, TValue Value>>
    // wouldn't be possible.
    // Technically it doesn't make any big difference: ValueTuple here is easily castable to
    // KeyValuePair and vice versa.
    public interface IReadOnlyDictionaryPlus<TKey>
        where TKey : notnull
    {
        IEnumerable<TKey> Keys { get; }
        IEnumerable<KeyValuePair<TKey, object?>> Items { get; }

        bool ContainsKey(TKey key);
        // Makes sense to keep "Untyped" part here, since otherwise
        // it causes generic method resolution conflicts on TKey argument
        bool TryGetValueUntyped(TKey key, out object? value);
    }

    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IReadOnlyDictionaryPlus<TKey, TValue> 
        : IReadOnlyDictionaryPlus<TKey>, IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    { }
    
    public sealed class ReadOnlyDictionaryPlus<TKey, TValue> : IReadOnlyDictionaryPlus<TKey, TValue>
        where TKey : notnull
    {
        private readonly IReadOnlyDictionary<TKey, TValue> _source;

        public int Count => _source.Count;
        public IEnumerable<TKey> Keys => _source.Keys;
        public IEnumerable<TValue> Values => _source.Values;
        IEnumerable<KeyValuePair<TKey, object?>> IReadOnlyDictionaryPlus<TKey>.Items 
            => this.Select(p => KeyValuePair.Create(p.Key, (object?) p.Value));
        public TValue this[TKey key] => _source[key];

        public ReadOnlyDictionaryPlus(IReadOnlyDictionary<TKey, TValue> source) => _source = source;

        public bool ContainsKey(TKey key) => _source.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => _source.TryGetValue(key, out value);
        public bool TryGetValueUntyped(TKey key, out object? value)
        {
            if (_source.TryGetValue(key, out var v)) {
                // ReSharper disable once HeapView.BoxingAllocation
                value = v;
                return true;
            }
            value = null;
            return false;
        }

        // Enumerators

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() 
            => _source.GetEnumerator();
    }
}
