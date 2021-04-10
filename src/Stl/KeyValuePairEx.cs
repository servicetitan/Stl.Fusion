using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl
{
    /// <summary>
    /// Extension methods for <see cref="ToKeyValuePair{TKey,TValue}"/>.
    /// </summary>
    public static class KeyValuePairEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(
            this (TKey Key, TValue Value) pair)
            => new(pair.Key, pair.Value);
    }
}
