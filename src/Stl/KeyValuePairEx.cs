using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl
{
    /// <summary>
    /// Extension methods and helpers for <see cref="ToKeyValuePair{TKey,TValue}"/>.
    /// </summary>
    public static class KeyValuePairEx
    {
        public static KeyValuePair<TKey, TValue> New<TKey, TValue>(TKey key, TValue value)
            => new(key, value);

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
        {
            key = self.Key;
            value = self.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(
            this (TKey Key, TValue Value) pair)
            => new(pair.Key, pair.Value);
    }
}
