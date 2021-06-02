#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Collections.Concurrent
{
    public static class ConcurrentCollectionsCompatEx
    {
        public static TValue GetOrAdd<TKey, TValue, TArg>(
            this ConcurrentDictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TArg, TValue> valueFactory,
            TArg argument)
            => dict.GetOrAdd(key, k => valueFactory(k, argument));
    }
}

#endif
