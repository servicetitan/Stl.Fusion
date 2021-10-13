using System;
using System.Collections.Generic;

namespace Stl.Collections
{
    public static class ReadOnlyMemoryExt
    {
        public static IEnumerable<T> AsEnumerable<T>(this ReadOnlyMemory<T> source)
        {
            for (var i = 0; i < source.Length; i++)
                yield return source.Span[i];
        }
    }
}
