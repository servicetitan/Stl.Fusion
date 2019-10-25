using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl;

public static partial class AsyncEnumerableEx
{
    public static async IAsyncEnumerable<Task<T>> Run<T>(
        this IEnumerable<Func<CancellationToken, Task<T>>> taskFactories,
        int? degreeOfParallelism = null,
        int dopProcessorCountMultiplier = 16,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        degreeOfParallelism ??= Environment.ProcessorCount * dopProcessorCountMultiplier;
        var runningTasks = new HashSet<Task<T>>();
        foreach (var taskFactory in taskFactories) {
            if (runningTasks.Count < degreeOfParallelism) {
                runningTasks.Add(taskFactory.Invoke(cancellationToken));
                continue;
            }
            var completedTask = await Task.WhenAny(runningTasks).ConfigureAwait(false);
            yield return completedTask;
        }
    }
}
