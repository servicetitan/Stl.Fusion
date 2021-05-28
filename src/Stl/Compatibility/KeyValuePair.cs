#if NETSTANDARD2_0

using System.Collections.Generic;

namespace Stl
{
    /// <summary>Creates instances of the <see cref="T:System.Collections.Generic.KeyValuePair`2" /> struct.</summary>
    public static class KeyValuePair
    {
        /// <summary>Creates a new key/value pair instance using provided values.</summary>
        /// <param name="key">The key of the new <see cref="T:System.Collections.Generic.KeyValuePair`2" /> to be created.</param>
        /// <param name="value">The value of the new <see cref="T:System.Collections.Generic.KeyValuePair`2" /> to be created.</param>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>A key/value pair containing the provided arguments as values.</returns>
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(
            TKey key,
            TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
        {
            key = self.Key;
            value = self.Value;
        }
    }
}

#endif