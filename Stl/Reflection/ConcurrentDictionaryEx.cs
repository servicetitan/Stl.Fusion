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
            public static Func<ConcurrentDictionary<TKey, TValue>, int> CapacityReader;

            static Cache()
            {
                var eSrc = Expression.Parameter(typeof(ConcurrentDictionary<TKey, TValue>), "source");
                var eTables = Expression.Field(eSrc, "_tables");
                var eBuckets = Expression.Field(eTables, "_buckets");
                var eCapacity = Expression.PropertyOrField(eBuckets, "Length");
                CapacityReader = (Func<ConcurrentDictionary<TKey, TValue>, int>)
                    Expression.Lambda(eCapacity, eSrc).Compile();
            }
        }

        public static int GetCapacity<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
            where TKey : notnull
            => Cache<TKey, TValue>.CapacityReader.Invoke(source);
    }
}
