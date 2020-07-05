using System;
using System.Collections.Generic;

namespace Stl.Collections
{
    public static class CollectionEx
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }

        // collection.TryGetOrDefault(keyOrIndex, @default) methods -- return default vs failing w/ exception

        public static T TryGetOrDefault<T>(this ReadOnlySpan<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];

        public static T TryGetOrDefault<T>(this Span<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];

        public static T TryGetOrDefault<T>(this T[] src, int index, T @default) =>
            index < 0 ? @default : index >= src.Length ? @default : src[index];

        public static T TryGetOrDefault<T>(this IList<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Count ? @default : src[index];

        public static T TryGetOrDefault<T>(this List<T> src, int index, T @default) =>
            index < 0 ? @default : index >= src.Count ? @default : src[index];

        public static TValue TryGetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> src, TKey key, TValue @default)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? value : @default;

        public static TValue TryGetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> src, TKey key, TValue @default)
            where TKey : notnull
            => src.TryGetValue(key, out var value) ? value : @default;

        public static T TryGetOrDefault<T>(this ISet<T> src, T item, T @default) =>
            src.Contains(item) ? item : @default;

        public static T TryGetOrDefault<T>(this HashSet<T> src, T item, T @default) =>
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
            if (!value.IsSome(out var v))
                return dictionary.Remove(key);
            dictionary[key] = v;
            return true;
        }

        public static bool SetOption<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Option<TValue> value)
            where TKey : notnull
        {
            if (!value.IsSome(out var v))
                return dictionary.Remove(key);
            dictionary[key] = v;
            return true;
        }
    }
}
