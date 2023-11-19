using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static class EnumerableCompatExt
{
#if NETSTANDARD2_0

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        => new(source);

#endif
#if !NET6_0_OR_GREATER

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var hashSet = new HashSet<TKey>();
        foreach (var item in source) {
            var key = keySelector(item);
            if (hashSet.Add(key))
                yield return item;
        }
    }

    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize < 1)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var chunk = new List<T>();
        foreach (var item in source) {
            chunk.Add(item);
            if (chunk.Count < chunkSize)
                continue;
            yield return chunk.ToArray();
            chunk.Clear();
        }
        if (chunk.Count > 0)
            yield return chunk.ToArray();
    }

#endif
}
