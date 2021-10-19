namespace Stl.Collections;

public static class ImmutableDictionaryExt
{
    public static ImmutableDictionary<TKey, TValue> SetItems<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> source,
        params (TKey Key, TValue Value)[] items)
        where TKey : notnull
        => source.SetItems(items.Select(i => KeyValuePair.Create(i.Key, i.Value)));
}
