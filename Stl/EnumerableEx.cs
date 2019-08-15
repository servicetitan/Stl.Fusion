using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Unsafe;
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

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> src, TKey key, TValue @default)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? value : @default;
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> src, TKey key, TValue @default)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? value : @default;

        public static T Get<T>(this ISet<T> src, T item, T @default) =>
            src.Contains(item) ? item : @default;
        public static T Get<T>(this HashSet<T> src, T item, T @default) =>
            src.Contains(item) ? item : @default;

        // collection.GetOption(keyOrIndex) methods -- return Option<TValue>

        public static Option<T> GetOption<T>(this ReadOnlySpan<T> src, int index) =>
            index < 0 ? default : index >= src.Length ? default : Option.Some(src[index]);
        public static Option<T> GetOption<T>(this Span<T> src, int index) =>
            index < 0 ? default : index >= src.Length ? default : Option.Some(src[index]);
        public static Option<T> GetOption<T>(this T[] src, int index) =>
            index < 0 ? default : index >= src.Length ? default : Option.Some(src[index]);

        public static Option<T> GetOption<T>(this IList<T> src, int index) =>
            index < 0 ? default : index >= src.Count ? default : Option.Some(src[index]);
        public static Option<T> GetOption<T>(this List<T> src, int index) =>
            index < 0 ? default : index >= src.Count ? default : Option.Some(src[index]);

        public static Option<TValue> GetOption<TKey, TValue>(this IDictionary<TKey, TValue> src, TKey key)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? Option.Some(value) : default;
        public static Option<TValue> GetOption<TKey, TValue>(this Dictionary<TKey, TValue> src, TKey key)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? Option.Some(value) : default;

        public static Option<T> GetOption<T>(this ISet<T> src, T item) =>
            src.Contains(item) ? Option.Some(item) : default;
        public static Option<T> GetOption<T>(this HashSet<T> src, T item) =>
            src.Contains(item) ? Option.Some(item) : default;

        // collection.SetOption(key, value) methods -- set or remove value

        public static bool SetOption<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Option<TValue> value)
            where TKey : notnull
        {
            if (!value.HasValue)
                return dictionary.Remove(key);
            dictionary[key] = value.ValueOrDefault();
            return true;
        }

        public static bool SetOption<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Option<TValue> value)
            where TKey : notnull
        {
            if (!value.HasValue)
                return dictionary.Remove(key);
            dictionary[key] = value.ValueOrDefault();
            return true;
        }

        // Enumerable extensions

        public static string ToDelimitedString<T>(this IEnumerable<T> source, string? delimiter = null)
            => string.Join(delimiter ?? ", ", source);

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
                if (processed.Contains(item))
                    continue;
                if (processing.Contains(item)) {
                    processing.Remove(item);
                    processed.Add(item);
                    yield return item;
                    continue;
                }
                processing.Add(item);
                stack.Push(item); // Pushing item in advance assuming there are dependencies
                var stackSize = stack.Count;
                foreach (var dependency in dependencySelector(item))
                    if (!processed.Contains(dependency)) {
                        if (processing.Contains(dependency))
                            throw Errors.CircularDependency(item);
                        stack.Push(dependency);
                    }
                if (stackSize == stack.Count) { // No unprocessed dependencies
                    stack.Pop(); // Popping item pushed in advance
                    processing.Remove(item);
                    processed.Add(item);
                    yield return item;
                }
            }
        }
    }
}
