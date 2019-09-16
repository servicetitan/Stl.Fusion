using System.Collections.Generic;

namespace Stl
{
    public static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(
            this (TKey Key, TValue Value) pair)
            => new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);

        public static KeyValuePair<TKey, TValue> New<TKey, TValue>(TKey key, TValue value)
            => new KeyValuePair<TKey, TValue>(key, value);
    }
}
