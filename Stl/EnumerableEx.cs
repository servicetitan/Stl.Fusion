using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Internal;

namespace Stl
{
    public static class EnumerableEx
    {
        // collection.Get(keyOrIndex, @default) methods -- return default vs failing w/ exception

        public static T Get<T>(this ReadOnlySpan<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];
        public static T Get<T>(this Span<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];
        public static T Get<T>(this T[] src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];

        public static T Get<T>(this IList<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Count ? @default : src[index];
        public static T Get<T>(this List<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Count ? @default : src[index];

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> src, TKey key, TValue @default) =>
            src.TryGetValue(key, out var value) ? value : @default;
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> src, TKey key, TValue @default) =>
            src.TryGetValue(key, out var value) ? value : @default;

        public static T Get<T>(this ISet<T> src, T item, T @default) =>
            src.Contains(item) ? item : @default;
        public static T Get<T>(this HashSet<T> src, T item, T @default) =>
            src.Contains(item) ? item : @default;

        public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] sequences)
        {
            if (sequences.Length == 0)
                return Enumerable.Empty<T>();
            var result = sequences[0];
            for (var i = 1; i < sequences.Length; i++) 
                result = result.Concat(sequences[i]);
            return result;
        }
        
        public static IEnumerable<T> OrderByDependency<T>(
            this IEnumerable<T> source, 
            Func<T, IEnumerable<T>> dependencySelector)
        {
            var processing = new HashSet<T>();
            var processed = new HashSet<T>();
            var stack = new Stack<T>(source);

            while (stack.TryPop(out var item)) {
                if (processing.Contains(item))
                    throw Errors.CircularDependency(item);
                if (processed.Contains(item))
                    continue;
                processing.Add(item);
                foreach (var dependency in dependencySelector(item))
                    stack.Push(dependency);
                processing.Remove(item);
                yield return item;
                processed.Add(item);
            }
        }
    }
}
