using Stl.Internal;
using Stl.OS;

namespace Stl.Async;

#pragma warning disable MA0064

static file class BatchProcessor
{
    public static readonly UnboundedChannelOptions DefaultChannelOptions = new UnboundedChannelOptions() {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = true,
    };
}

public class BatchProcessor<T, TResult>(Channel<BatchProcessor<T, TResult>.Item> queue) : ProcessorBase
{
    protected Channel<Item> Queue = queue;
    protected int PlannedWorkerCount;
    protected HashSet<Task> Workers = new();
    protected Task? WorkerCollectorTask;
    protected object WorkerLock => Workers;

    // Metrics
    protected readonly object MetricsLock = new();
    protected int ProcessedItemCount;
    protected long MinQueueDurationInTicks = long.MaxValue;
    protected CpuTimestamp LastAdjustmentAt;

    // Settings
    public int MinWorkerCount { get; set; } = 1;
    public int MaxWorkerCount { get; set; } = HardwareInfo.GetProcessorCountFactor(2);
    public int AdjustmentInterval { get; set; } = 16;
    public TimeSpan AdjustmentPeriod { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan KillWorkerAt { get; set; } = TimeSpan.FromMilliseconds(1);
    public TimeSpan Kill8WorkersAt { get; set; } = TimeSpan.FromMilliseconds(0.1);
    public TimeSpan AddWorkerAt { get; set; } = TimeSpan.FromMilliseconds(20);
    public TimeSpan Add4WorkersAt { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan WorkerCollectionPeriod { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 256;
    public Func<List<Item>, CancellationToken, Task> Implementation { get; set; } = (_, _) => Task.CompletedTask;

    public BatchProcessor()
        : this(Channel.CreateUnbounded<Item>(BatchProcessor.DefaultChannelOptions))
    { }

    protected override Task DisposeAsyncCore()
    {
        Queue.Writer.TryComplete();
        lock (WorkerLock)
            return Workers.Count != 0
                ? Task.WhenAll(Workers.ToArray())
                : Task.CompletedTask;
    }

    public async Task<TResult> Process(T input, CancellationToken cancellationToken = default)
    {
        if (PlannedWorkerCount == 0) {
            lock (WorkerLock)
                if (PlannedWorkerCount == 0 && !StopToken.IsCancellationRequested)
                    _ = UpdateWorkerCount(MinWorkerCount);
        }
        var item = new Item(input, cancellationToken);
        await Queue.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        var result = await item.ResultTask.ConfigureAwait(false);
        var workerDelta = UpdateMetrics(item);
        await UpdateWorkerCount(workerDelta).ConfigureAwait(false);
        return result;
    }

    public int GetWorkerCount()
    {
        lock (WorkerLock)
            return Workers.Count;
    }

    public int GetPlannedWorkerCount()
    {
        lock (WorkerLock)
            return PlannedWorkerCount;
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        while (true) {
            ValueTask updateTask;
            lock (WorkerLock)
                updateTask = UpdateWorkerCount(MinWorkerCount - PlannedWorkerCount);

            await updateTask.ConfigureAwait(false);
            lock (WorkerLock) {
                if (Workers.Count == MinWorkerCount) {
                    lock (MetricsLock) {
                        ProcessedItemCount = 0;
                        MinQueueDurationInTicks = long.MaxValue;
                    }
                    return;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(0.05), cancellationToken).ConfigureAwait(false);
        }
    }

    // Protected methods

    protected int UpdateMetrics(Item? item = null)
    {
        if (item != null && item.DequeuedAt == default)
            return 0; // Something is off / the item was never processed

        var queueDuration = item != null
            ? (item.DequeuedAt - item.QueuedAt).Ticks
            : 0;
        TimeSpan minQueueDuration;
        lock (MetricsLock) {
            ProcessedItemCount += 1;
            MinQueueDurationInTicks = Math.Min(queueDuration, MinQueueDurationInTicks);
            var now = CpuTimestamp.Now;
            if (item != null && ProcessedItemCount < AdjustmentInterval)
                return 0;

            minQueueDuration = TimeSpan.FromTicks(MinQueueDurationInTicks);
            ProcessedItemCount = 0;
            MinQueueDurationInTicks = long.MaxValue;
            if (now - LastAdjustmentAt < AdjustmentPeriod)
                return 0;

            LastAdjustmentAt = now;
        }

        if (minQueueDuration > Add4WorkersAt)
            return 4;
        if (minQueueDuration > AddWorkerAt)
            return 1;
        if (minQueueDuration < Kill8WorkersAt)
            return -8;
        if (minQueueDuration < KillWorkerAt)
            return -1;
        return 0;
    }

    protected async ValueTask UpdateWorkerCount(int delta)
    {
        if (delta == 0)
            return;

        lock (WorkerLock) {
            var oldPlannedWorkerCount = PlannedWorkerCount;
            PlannedWorkerCount = (oldPlannedWorkerCount + delta).Clamp(MinWorkerCount, MaxWorkerCount);
            delta = PlannedWorkerCount - oldPlannedWorkerCount;
            if (StopToken.IsCancellationRequested) {
                PlannedWorkerCount = 0;
                Queue.Writer.TryComplete();
                return;
            }
        }
        if (delta == 0)
            return;

        if (delta < 0) { // -> remove workers
            try {
                for (; delta < 0; delta++)
                    await Queue.Writer.WriteAsync(Item.WorkerKiller).ConfigureAwait(false);
            }
            catch {
                // Intended: we can't write only if Queue is completed
            }
        }
        else { // workerDelta > 0 -> add workers
            using var flowSuppressor = ExecutionContextExt.SuppressFlow();
            lock (WorkerLock) {
                WorkerCollectorTask ??= Task.Run(RunWorkerCollector, CancellationToken.None);
                for (; delta > 0; delta--) {
                    var workerTask = Task.Run(RunWorker, CancellationToken.None);
                    Workers.Add(workerTask);
                    _ = workerTask.ContinueWith(static (task, state) => {
                        var self = (BatchProcessor<T, TResult>)state!;
                        lock (self.WorkerLock)
                            self.Workers.Remove(task);
                    }, this, TaskScheduler.Default);
                }
            }
        }
    }

    protected virtual async Task RunWorkerCollector()
    {
        var longCycle = WorkerCollectionPeriod;
        var shortCycle = TimeSpan.FromSeconds(WorkerCollectionPeriod.TotalSeconds / 4);
        var nextCycle = longCycle;
        while (!StopToken.IsCancellationRequested)
            try {
                await Task.Delay(nextCycle, StopToken).ConfigureAwait(false);
                var workerDelta = UpdateMetrics();
                await UpdateWorkerCount(workerDelta).ConfigureAwait(false);
                nextCycle = workerDelta < 0 ? shortCycle : longCycle;
            }
            catch {
                nextCycle = shortCycle;
            }
    }

    protected virtual async Task RunWorker()
    {
        var reader = Queue.Reader;
        var batch = new List<Item>(BatchSize);
        try {
            while (await reader.WaitToReadAsync().ConfigureAwait(false)) {
                while (reader.TryRead(out var item)) {
                    if (ReferenceEquals(item, Item.WorkerKiller)) {
                        await ProcessBatch(batch).ConfigureAwait(false);
                        return;
                    }
                    item.DequeuedAt = CpuTimestamp.Now;
                    batch.Add(item);
                    if (batch.Count >= BatchSize)
                        await ProcessBatch(batch).ConfigureAwait(false);
                }
                await ProcessBatch(batch).ConfigureAwait(false);
            }
        }
        finally {
            await ProcessBatch(batch).ConfigureAwait(false);
        }
    }

    protected Task ProcessBatch(List<Item> batch)
    {
        if (StopToken.IsCancellationRequested) {
            foreach (var item in batch) {
                var cancellationToken = item.CancellationToken;
                if (!cancellationToken.IsCancellationRequested)
                    cancellationToken = StopToken;
                item.ResultSource.TrySetCanceled(cancellationToken);
            }
            batch.Clear();
            return Task.CompletedTask;
        }
        for (var i = batch.Count - 1; i >= 0; i--) {
            var item = batch[i];
            if (!item.CancellationToken.IsCancellationRequested)
                continue;

            item.ResultSource.TrySetCanceled();
            batch.RemoveAt(i);
        }
        if (batch.Count == 0)
            return Task.CompletedTask;

        Task resultTask;
        try {
            resultTask = Implementation.Invoke(batch, StopToken);
        }
        catch (Exception e) {
            return CompleteProcessBatch(batch, e);
        }
        return resultTask.IsCompletedSuccessfully()
            ? CompleteProcessBatch(batch)
            : CompleteProcessBatchAsync(batch, resultTask);
    }

    // Private methods

    private async Task CompleteProcessBatchAsync(List<Item> batch, Task resultTask)
    {
        var error = (Exception?)null;
        try {
            await resultTask.ConfigureAwait(false);
        }
        catch (Exception e) {
            error = e;
        }
        _ = CompleteProcessBatch(batch, error);
    }

    private Task CompleteProcessBatch(List<Item> batch, Exception? error = null)
    {
        var completedAt = CpuTimestamp.Now;
        foreach (var item in batch) {
            item.CompletedAt = completedAt;
            if (error == null)
                item.SetError(Errors.UnprocessedBatchItem());
            else if (error is OperationCanceledException)
                item.SetCancelled(StopToken);
            else
                item.SetError(error);
        }
        batch.Clear();
        return Task.CompletedTask;
    }

    // Nested types

    public class Item(T input, TaskCompletionSource<TResult> resultSource, CancellationToken cancellationToken)
    {
        private static readonly TaskCompletionSource<TResult> KillSource
            = TaskCompletionSourceExt.New<TResult>()
                .WithException(Errors.InternalError("Something is off: you should never see the KillItem in batches."));

        public static readonly Item WorkerKiller = new(default!, KillSource, default);

        public readonly T Input = input;
        public readonly TaskCompletionSource<TResult> ResultSource = resultSource;
        public readonly CancellationToken CancellationToken = cancellationToken;
        public readonly CpuTimestamp QueuedAt = CpuTimestamp.Now;
        public CpuTimestamp DequeuedAt;
        public CpuTimestamp CompletedAt;
        public Task<TResult> ResultTask => ResultSource.Task;

        public Item(T input, CancellationToken cancellationToken)
            : this(input, TaskCompletionSourceExt.New<TResult>(), cancellationToken)
        { }

        public override string ToString()
            => $"{GetType().GetName()}({Input}, {CancellationToken}) -> {ResultTask}";

        public void SetResult(TResult result)
            => ResultSource.TrySetResult(result);
        public void SetResult(Result<TResult> result)
            => ResultSource.TrySetFromResult(result);
        public void SetResult(Result<TResult> result, CancellationToken cancellationToken)
            => ResultSource.TrySetFromResult(result, cancellationToken);

        public void SetError(Exception error)
            => ResultSource.TrySetException(error);

        public void SetCancelled(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                ResultSource.TrySetCanceled(cancellationToken);
            else
                ResultSource.TrySetCanceled();
        }

        public bool SetCancelledIfCancelled()
        {
            if (!CancellationToken.IsCancellationRequested)
                return false;

            ResultSource.TrySetCanceled(CancellationToken);
            return true;
        }
    }
}
