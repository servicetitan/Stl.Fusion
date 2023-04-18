using System.Reflection.Emit;

namespace Stl.Collections;

public static class ConcurrentDictionaryExt
{
    private static class Cache<TKey, TValue>
        where TKey : notnull
    {
        public static readonly Func<ConcurrentDictionary<TKey, TValue>, int> CapacityReader;

        static Cache()
        {
#if !NETSTANDARD2_0
            var fTablesName = "_tables";
            var fBucketsName = "_buckets";
#else
            var fTablesName = "m_tables";
            var fBucketsName = "m_buckets";
#endif
            var fTables = typeof(ConcurrentDictionary<TKey, TValue>)
                .GetField(fTablesName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var fBuckets = fTables.FieldType
                .GetField(fBucketsName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pLength = fBuckets.FieldType
                .GetProperty("Length", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            var m = new DynamicMethod("_CapacityReader",
                typeof(int), new [] { typeof(ConcurrentDictionary<TKey, TValue>)},
                true);
            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fTables);
            il.Emit(OpCodes.Ldfld, fBuckets);
            il.Emit(OpCodes.Callvirt, pLength.GetMethod!);
            il.Emit(OpCodes.Ret);
            CapacityReader = (Func<ConcurrentDictionary<TKey, TValue>, int>)m.CreateDelegate(typeof(Func<ConcurrentDictionary<TKey, TValue>, int>));
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
