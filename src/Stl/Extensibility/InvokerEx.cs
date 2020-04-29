using System;

namespace Stl.Extensibility
{
    public static class InvokerEx
    {
        public static ReadOnlyMemory<T> WithoutAncestorsOf<T>(this ReadOnlyMemory<T> source, Type type)
            where T : class
        {
            var result = new T[source.Length];
            for (var i = 0; i < source.Span.Length; i++) {
                var item = source.Span[i];
                if (item != null && type.IsSubclassOf(item.GetType()))
                    item = null!;
                result[i] = item!;
            }
            return result;
        }
    }
}
