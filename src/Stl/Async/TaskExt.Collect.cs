using Stl.OS;

namespace Stl.Async;

#pragma warning disable MA0004

public static partial class TaskExt
{
    private static int MaxConcurrencyLevel { get; set; } = 2048;

    // Collect - a "gated" version of Task.WhenAll ensuring
    // only up to the concurrencyLevel tasks are running at once.

    public static async Task<T[]> Collect<T>(this IEnumerable<Task<T>> tasks, int concurrencyLevel = -1)
    {
        if (concurrencyLevel < 0) // Concurrency level = auto
            concurrencyLevel = HardwareInfo.GetProcessorCountFactor(16, 16);
        else if (concurrencyLevel == 0)
            concurrencyLevel = MaxConcurrencyLevel;

        var collector = Collector.Rent();
        foreach (var task in tasks) {
            collector.Add(task);
            var availableSlotCount = concurrencyLevel - collector.RunningTaskCount;
            if (availableSlotCount < 0)
                await collector.Complete(-availableSlotCount).ConfigureAwait(false);
        }
        return await collector.ToResultAndRelease<T>().ConfigureAwait(false);
    }

    public static async Task Collect(this IEnumerable<Task> tasks, int concurrencyLevel = -1)
    {
        if (concurrencyLevel < 0) // Concurrency level = auto
            concurrencyLevel = HardwareInfo.GetProcessorCountFactor(16, 16);
        else if (concurrencyLevel == 0)
            concurrencyLevel = MaxConcurrencyLevel;

        var collector = Collector.Rent();
        foreach (var task in tasks) {
            collector.Add(task);
            var availableSlotCount = concurrencyLevel - collector.RunningTaskCount;
            if (availableSlotCount < 0)
                await collector.Complete(-availableSlotCount).ConfigureAwait(false);
        }
        await collector.ToResultAndRelease().ConfigureAwait(false);
    }
    
    // Nested types

    private sealed class Collector
    {
        private static readonly int CacheSize = HardwareInfo.GetProcessorCountPo2Factor(16, 16); 
        private static readonly ConcurrentBag<Collector> Cache = new();
    
        public static Collector Rent() 
            => Cache.TryTake(out var cached) ? cached : new Collector();

        public readonly List<Task> Tasks = new();
        public readonly Channel<Task> CompletedTasks = 
            Channel.CreateUnbounded<Task>(new UnboundedChannelOptions() {
                SingleReader = true, 
                SingleWriter = false
            });
        public int RunningTaskCount;

        private Collector() { }

        public void Add(Task task)
        {
            RunningTaskCount++;
            Tasks.Add(task);

            // NOTE(AY):
            // Channel.CreateUnbounded w/ SingleReader = true ensures that
            // WriteAsync always completes synchronously, so it's safe to
            // just "throw" EnqueueCompletion & don't "track" its result task
            // assuming we reference Task from somewhere. 
            if (task.IsCompleted)
                CompletedTasks.Writer.WriteAsync(task); 
            else {
                task.ContinueWith(static (_, entry1) => {
                    var entry = (IncompleteEntry)entry1;
                    entry.Collector.CompletedTasks.Writer.WriteAsync(entry.Task);
                }, new IncompleteEntry(this, task));
            }
        }

        public async ValueTask Complete(int taskCount)
        {
            while (taskCount-- > 0) {
                var task = await CompletedTasks.Reader.ReadAsync().ConfigureAwait(false);
                RunningTaskCount--;
                await task; // It must be already completed, so no ConfigureAwait(false) here
            }
        }

        public async ValueTask ToResultAndRelease()
        {
            // Waiting for remaining tasks
            await Complete(RunningTaskCount).ConfigureAwait(false);
            Release();
        }

        public async ValueTask<T[]> ToResultAndRelease<T>()
        {
            // Waiting for remaining tasks
            await Complete(RunningTaskCount).ConfigureAwait(false);
            var result = new T[Tasks.Count];
            for (var i = 0; i < Tasks.Count; i++) {
                var task = (Task<T>)Tasks[i];
                result[i] = await task; // It must be already completed, so no ConfigureAwait(false) here 
            }
            Release();
            return result;
        }

        private void Release()
        {
            if (RunningTaskCount != 0 || Cache.Count >= CacheSize)
                return;

            Tasks.Clear();
            Cache.Add(this);
        }
    }

    private sealed record IncompleteEntry(Collector Collector, Task Task);
}
