#if NETSTANDARD2_0

using System.Collections.Generic;

namespace System.Linq
{
    internal static class EnumerableCompatEx
    {
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            return new HashSet<TSource>(source);
        }
    }
}

#endif
