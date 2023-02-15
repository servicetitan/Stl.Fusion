using System.Linq.Expressions;

namespace Stl.Concurrency;

public static class ConcurrentDictionaryExt
{
    private static class Cache<TKey, TValue>
        where TKey : notnull
    {
        public static readonly Func<ConcurrentDictionary<TKey, TValue>, int> CapacityReader;

        static Cache()
        {
            var eSrc = Expression.Parameter(typeof(ConcurrentDictionary<TKey, TValue>), "source");

#if !NETSTANDARD2_0
            var body = Expression.PropertyOrField(
                Expression.Field(Expression.Field(eSrc, "_tables"), "_buckets"),
                "Length");
#else
            var body = Expression.PropertyOrField(
                Expression.Field(Expression.Field(eSrc, "m_tables"), "m_buckets"),
                "Length");
#endif

            CapacityReader = (Func<ConcurrentDictionary<TKey, TValue>, int>)
                Expression.Lambda(body, eSrc).Compile();
        }
    }

    public static int GetCapacity<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source)
        where TKey : notnull
        => Cache<TKey, TValue>.CapacityReader(source);

    public static bool TryRemove<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key, TValue value)
        where TKey : notnull
        // Based on:
        // - https://devblogs.microsoft.com/pfxteam/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
        => ((ICollection<KeyValuePair<TKey, TValue>>) dictionary)
            .Remove(KeyValuePair.Create(key, value));

    public static int Increment<TKey>(this ConcurrentDictionary<TKey, int> dictionary, TKey key)
        where TKey : notnull
    {
        while (true) {
            if (dictionary.TryGetValue(key, out var value)) {
                var newValue = value + 1;
                if (dictionary.TryUpdate(key, newValue, value))
                    return newValue;
            }
            else {
                if (dictionary.TryAdd(key, 1))
                    return 1;
            }
        }
    }

    public static int Decrement<TKey>(this ConcurrentDictionary<TKey, int> dictionary, TKey key)
        where TKey : notnull
    {
        while (true) {
            var value = dictionary[key];
            if (value > 1) {
                var newValue = value - 1;
                if (dictionary.TryUpdate(key, newValue, value))
                    return newValue;
            }
            else {
                if (dictionary.TryRemove(key, value))
                    return 0;
            }
        }
    }
}
