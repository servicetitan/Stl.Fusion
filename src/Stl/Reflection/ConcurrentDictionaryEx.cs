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
    }
}
