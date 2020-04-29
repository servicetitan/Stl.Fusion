using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Stl.Reflection
{
    public static class ConcurrentDictionaryEx
    {
        private static class Cache<TKey, TValue>
            where TKey : notnull
        {
            public static readonly Func<ConcurrentDictionary<TKey, TValue>, int> CapacityReader;

            static Cache()
            {
                var eSrc = Expression.Parameter(typeof(ConcurrentDictionary<TKey, TValue>), "source");
                var body = Expression.PropertyOrField(
                    Expression.Field(Expression.Field(eSrc, "_tables"), "_buckets"), 
                    "Length");
                CapacityReader = (Func<ConcurrentDictionary<TKey, TValue>, int>)
                    Expression.Lambda(body, eSrc).Compile();
            }
        }

        public static int GetCapacity<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
            where TKey : notnull
            => Cache<TKey, TValue>.CapacityReader.Invoke(source);

        public static TValue GetOrAddChecked<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> valueFactory)
            where TKey : notnull 
            => dictionary.TryGetValue(key, out var v) 
                ? v 
                : dictionary.GetOrAdd(key, valueFactory);

        public static TValue GetOrAddChecked<TKey, TValue, TArg>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TArg, TValue> valueFactory,
            TArg factoryArgument)
            where TKey : notnull 
            => dictionary.TryGetValue(key, out var v) 
                ? v 
                : dictionary.GetOrAdd(key, valueFactory, factoryArgument);
    }
}
