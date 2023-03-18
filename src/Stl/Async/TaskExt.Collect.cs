namespace Stl.Async;

#pragma warning disable MA0004

public static partial class TaskExt
{
    // Collect - a bit more user-friendly version of Task.WhenAll

    public static Task<T[]> Collect<T>(this IEnumerable<Task<T>> tasks, int concurrency = 0) 
        => concurrency <= 0 ? Task.WhenAll(tasks) : CollectConcurrently(tasks, concurrency);

    public static Task Collect(this IEnumerable<Task> tasks, int concurrency = 0)
        => concurrency <= 0 ? Task.WhenAll(tasks) : CollectConcurrently(tasks, concurrency);

    private static async Task<T[]> CollectConcurrently<T>(this IEnumerable<Task<T>> tasks, int concurrency)
    {
        var list = new List<Task<T>>();
        var runningCount = 0;
        var i = 0;
        foreach (var task in tasks) {
            list.Add(task);
            if (concurrency > runningCount) {
                runningCount++;
            }
            else {
                try {
                    await list[i++].ConfigureAwait(false);
                }
                catch {
                    // Intended
                }
            }
        }
        return await Task.WhenAll(list).ConfigureAwait(false);
    }

    private static async Task CollectConcurrently(this IEnumerable<Task> tasks, int concurrency)
    {
        var list = new List<Task>();
        var runningCount = 0;
        var i = 0;
        foreach (var task in tasks) {
            list.Add(task);
            if (concurrency > runningCount) {
                runningCount++;
            }
            else {
                try {
                    await list[i++].ConfigureAwait(false);
                }
                catch {
                    // Intended
                }
            }
        }
        await Task.WhenAll(list).ConfigureAwait(false);
    }
}
