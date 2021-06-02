#if NETSTANDARD2_0

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class EnumerableCompatEx
    {
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
            => new(source);
    }
}

#endif
