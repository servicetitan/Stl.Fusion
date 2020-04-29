namespace Stl.Purifier
{
    public static class FunctionEx
    {
        public static bool Invalidate(this IFunction function, object key)
            => function.TryGetCached(key)?.Invalidate() ?? false;

        public static bool Invalidate<TKey>(this IFunction<TKey> function, TKey key)
            where TKey : notnull 
            => function.TryGetCached(key)?.Invalidate() ?? false;
    }
}
