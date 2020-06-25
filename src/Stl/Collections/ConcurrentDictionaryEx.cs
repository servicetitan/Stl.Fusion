using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Stl.Collections
{
    public static class ConcurrentDictionaryEx
    {
        public static bool TryRemove<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary, 
            TKey key, TValue value)
            where TKey : notnull
            // Based on:
            // - https://devblogs.microsoft.com/pfxteam/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
            => ((ICollection<KeyValuePair<TKey, TValue>>) dictionary)
                .Remove(KeyValuePair.Create(key, value));

        public static int Increment<TKey>(this ConcurrentDictionary<TKey, int> dictionary, TKey key)
            where TKey : notnull
        {
            while (true) {
                if (dictionary.TryGetValue(key, out var value)) {
                    var newValue = value + 1;
                    if (dictionary.TryUpdate(key, newValue, value))
                        return newValue;
                }
                else {
                    if (dictionary.TryAdd(key, 1))
                        return 1;
                }
            }
        }

        public static int Decrement<TKey>(this ConcurrentDictionary<TKey, int> dictionary, TKey key)
            where TKey : notnull
        {
            while (true) {
                var value = dictionary[key];
                if (value > 1) {
                    var newValue = value - 1;
                    if (dictionary.TryUpdate(key, newValue, value))
                        return newValue;
                }
                else {
                    if (dictionary.TryRemove(key, value))
                        return 0;
                }
            }
        }
    }
}
