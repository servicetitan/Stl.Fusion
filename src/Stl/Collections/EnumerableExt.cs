using Stl.Internal;

namespace Stl.Collections;

public static class EnumerableExt
{
    // Regular static methods

    public static IEnumerable<T> One<T>(T value) => Enumerable.Repeat(value, 1);

    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] sequences)
    {
        if (sequences.Length == 0)
            return Enumerable.Empty<T>();
        var result = sequences[0];
        for (var i = 1; i < sequences.Length; i++)
            result = result.Concat(sequences[i]);
        return result;
    }

    // Extensions

    public static IEnumerable<T> Apply<T>(this IEnumerable<T> source, Action<T> action)
    {
        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var item in source)
            action(item);
        // ReSharper disable once PossibleMultipleEnumeration
        return source;
    }

    // ToDelimitedString

    public static string ToDelimitedString<T>(this IEnumerable<T> source, string? delimiter = null)
        => string.Join(delimiter ?? ", ", source);

    // OrderByDependency

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

    // Collect - a bit more user-friendly Task.WhenAll

    public static Task<List<T>> Collect<T>(
        this IEnumerable<Task<T>> tasks,
        CancellationToken cancellationToken = default)
        => tasks.Collect(64, cancellationToken);

    public static async Task<List<T>> Collect<T>(
        this IEnumerable<Task<T>> tasks,
        int chunkSize,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        foreach (var chunk in tasks.Chunk(chunkSize)) {
            var chunkResults = await Task.WhenAll(chunk).ConfigureAwait(false);
            results.AddRange(chunkResults);
        }
        return results;
    }

    public static Task Collect(
        this IEnumerable<Task> tasks,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(tasks);
}
