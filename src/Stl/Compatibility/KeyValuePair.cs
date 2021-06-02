#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic
{
    /// <summary>Creates instances of the <see cref="T:System.Collections.Generic.KeyValuePair`2" /> struct.</summary>
    public static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            => new(key, value);

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> self, out TKey key, out TValue value)
        {
            key = self.Key;
            value = self.Value;
        }
    }
}

#endif
