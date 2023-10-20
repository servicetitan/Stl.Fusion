namespace Stl.Async;

#pragma warning disable MA0004

public static partial class TaskExt
{
    // Collect - a bit more user-friendly version of Task.WhenAll

    public static Task<T[]> Collect<T>(this IEnumerable<Task<T>> tasks, int concurrency = 0)
        => concurrency <= 0 ? Task.WhenAll(tasks) : CollectConcurrently(tasks, concurrency);

    public static Task Collect(this IEnumerable<Task> tasks, int concurrency = 0)
        => concurrency <= 0 ? Task.WhenAll(tasks) : CollectConcurrently(tasks, concurrency);

    public static Task<Result<T>[]> CollectResults<T>(this IEnumerable<Task<T>> tasks, int concurrency = 0)
        => concurrency <= 0 ? ToResults(tasks.ToList()) : CollectResultsConcurrently(tasks, concurrency);

    // Private methods

    private static async Task<T[]> CollectConcurrently<T>(this IEnumerable<Task<T>> tasks, int concurrency)
    {
        var list = new List<Task<T>>();
        var runningCount = 0;
        var i = 0;
        foreach (var task in tasks) {
            list.Add(task);
            if (concurrency > runningCount)
                runningCount++;
            else
                await list[i++].SilentAwait(false);
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
            if (concurrency > runningCount)
                runningCount++;
            else
                await list[i++].SilentAwait(false);
        }
        await Task.WhenAll(list).ConfigureAwait(false);
    }

    private static async Task<Result<T>[]> CollectResultsConcurrently<T>(this IEnumerable<Task<T>> tasks, int concurrency)
    {
        var list = new List<Task<T>>();
        var runningCount = 0;
        var i = 0;
        foreach (var task in tasks) {
            list.Add(task);
            if (concurrency > runningCount)
                runningCount++;
            else
                await list[i++].SilentAwait(false);
        }
        return await ToResults(list).ConfigureAwait(false);
    }

    private static async Task<Result<T>[]> ToResults<T>(List<Task<T>> list)
    {
        await Task.WhenAll(list).SilentAwait(false);
        var result = new Result<T>[list.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = list[i].ToResultSynchronously();
        return result;
    }
}
