namespace Stl.Collections;

public static class CollectionExt
{
    // AddRange

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
            collection.Add(item);
    }

    // SetOrRemove

    public static bool SetOrRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Option<TValue> value)
        where TKey : notnull
    {
        if (!value.IsSome(out var v))
            return dictionary.Remove(key);
        dictionary[key] = v;
        return true;
    }

    public static bool SetOrRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Option<TValue> value)
        where TKey : notnull
    {
        if (!value.IsSome(out var v))
            return dictionary.Remove(key);
        dictionary[key] = v;
        return true;
    }

    public static ImmutableDictionary<TKey, TValue> SetOrRemove<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        TKey key,
        Option<TValue> value)
        where TKey : notnull
        => value.IsSome(out var v) ? dictionary.SetItem(key, v) : dictionary.Remove(key);
}
