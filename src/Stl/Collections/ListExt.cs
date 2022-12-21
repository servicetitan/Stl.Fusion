namespace Stl.Collections;

public static class ListExt
{
    public static void Expand<T>(this List<T> list, int minCount)
    {
        if (list.Count >= minCount)
            return;

        for (var i = list.Count; i < minCount; i++)
            list.Add(default!);
    }

    public static void ExpandAndSet<T>(this List<T> list, int index, T value)
    {
        list.Expand(index + 1);
        list[index] = value;
    }
}
