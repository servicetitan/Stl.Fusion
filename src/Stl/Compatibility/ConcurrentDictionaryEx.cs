#if NETSTANDARD2_0

namespace System.Collections.Concurrent
{
    public static class ConcurrentDictionaryEx
    {
        public static TValue GetOrAdd<TKey, TValue, TArg>(
            this ConcurrentDictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TArg, TValue> valueFactory,
            TArg factoryArgument)
        {
            Func<TKey, TValue> valueFactory2 = k => valueFactory(k, factoryArgument);
            return dict.GetOrAdd(key, valueFactory2);
        }
    }
}

#endif
