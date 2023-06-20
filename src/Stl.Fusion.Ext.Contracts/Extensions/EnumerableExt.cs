namespace Stl.Fusion.Extensions;

public static class EnumerableExt
{
    public static IEnumerable<T> OrderBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        SortDirection keySortDirection)
        => keySortDirection == SortDirection.Ascending
            ? source.OrderBy(keySelector)
            : source.OrderByDescending(keySelector);

    public static IEnumerable<T> TakePage<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        PageRef<TKey> pageRef,
        SortDirection keySortDirection = SortDirection.Ascending)
    {
        if (pageRef.After.IsSome(out var after)) {
            var comparer = Comparer<TKey>.Default;
            if (keySortDirection == SortDirection.Ascending)
                source = source.Where(i => comparer.Compare(keySelector(i), after) > 0);
            else
                source = source.Where(i => comparer.Compare(keySelector(i), after) < 0);
        }
        return source.Take(pageRef.Count);
    }

    public static IEnumerable<T> OrderByAndTakePage<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        PageRef<TKey> pageRef,
        SortDirection keySortDirection = SortDirection.Ascending)
        => source
            .OrderBy(keySelector, keySortDirection)
            .TakePage(keySelector, pageRef, keySortDirection);
}
