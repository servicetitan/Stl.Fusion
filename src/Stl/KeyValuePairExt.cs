namespace Stl;

/// <summary>
/// Extension methods and helpers for <see cref="ToKeyValuePair{TKey,TValue}"/>.
/// </summary>
public static class KeyValuePairExt
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this (TKey Key, TValue Value) pair)
        => new(pair.Key, pair.Value);
}
