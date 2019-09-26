using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl
{
    public static class KeyValuePair
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(
            this (TKey Key, TValue Value) pair)
            => new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<TKey, TValue> New<TKey, TValue>(TKey key, TValue value)
            => new KeyValuePair<TKey, TValue>(key, value);
    }
}
