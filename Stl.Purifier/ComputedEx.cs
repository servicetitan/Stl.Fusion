namespace Stl.Purifier
{
    public static class ComputedEx
    {
        public static ComputedRef<TKey> ToRef<TKey>(
            this IComputedWithTypedInput<TKey> target)
            where TKey : notnull
            => new ComputedRef<TKey>(target.Function, target.Input, target.Tag);
    }
}
