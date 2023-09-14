using Stl.Internal;

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
    private volatile IBatchProcessorWorkerPolicy _workerPolicy = BatchProcessorWorkerPolicy.Default;

    protected readonly Channel<Item> Queue = queue;
    protected int PlannedWorkerCount;
    protected HashSet<Task> Workers = new();
    protected Task? WorkerCollectorTask;

    // Statistics
    protected RingBuffer<TimeSpan> RecentReports = new(7);
    protected CpuTimestamp CooldownEndsAt;

    // Settings
    public int BatchSize { get; set; } = 256;
    public Func<List<Item>, CancellationToken, Task> Implementation { get; set; } = (_, _) => Task.CompletedTask;
    public IBatchProcessorWorkerPolicy WorkerPolicy {
        get => _workerPolicy;
        set => Interlocked.Exchange(ref _workerPolicy, value);
    }
    public ILogger? Log { get; set; }

    public BatchProcessor()
        : this(Channel.CreateUnbounded<Item>(BatchProcessor.DefaultChannelOptions))
    { }

    protected override Task DisposeAsyncCore()
    {
        // This method starts inside lock (Lock) block, so no need to lock here
        Queue.Writer.TryComplete();
        return Workers.Count != 0
            ? Task.WhenAll(Workers.ToArray())
            : Task.CompletedTask;
    }

    public Task<TResult> Process(T input, CancellationToken cancellationToken = default)
    {
        if (PlannedWorkerCount == 0) {
            lock (Lock)
                if (PlannedWorkerCount == 0 && !StopToken.IsCancellationRequested) {
                    var minWorkerCount = WorkerPolicy.MinWorkerCount;
                    if (minWorkerCount < 1)
                        throw Errors.InternalError("WorkerPolicy.MinWorkerCount < 1");
                    _ = AddOrRemoveWorkers(WorkerPolicy.MinWorkerCount);
                }
        }

        var item = new Item(input, cancellationToken);
        return Queue.Writer.TryWrite(item)
            ? item.ResultTask
            : ProcessAsync(item, cancellationToken);

        async Task<TResult> ProcessAsync(Item item1, CancellationToken cancellationToken1) {
            await Queue.Writer.WriteAsync(item1, cancellationToken1).ConfigureAwait(false);
            return await item.ResultTask.ConfigureAwait(false);
        }
    }

    public int GetWorkerCount()
    {
        lock (Lock)
            return Workers.Count;
    }

    public int GetPlannedWorkerCount()
    {
        lock (Lock)
            return PlannedWorkerCount;
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        while (true) {
            var wp = WorkerPolicy;
            await AddOrRemoveWorkers(wp.MinWorkerCount - GetPlannedWorkerCount(), true).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(0.05), cancellationToken).ConfigureAwait(false);
            lock (Lock) {
                if (Workers.Count == wp.MinWorkerCount && PlannedWorkerCount == wp.MinWorkerCount) {
                    RecentReports.Clear();
                    CooldownEndsAt = default;
                    return;
                }
            }
        }
    }

    // Protected methods

    protected void ProcessWorkerReport(TimeSpan workerMinQueueTime)
    {
        var minQueueTime = TimeSpan.MaxValue;
        lock (Lock) {
            RecentReports.PushHeadAndMoveTailIfFull(workerMinQueueTime);
            var now = CpuTimestamp.Now;
            if (CooldownEndsAt > now)
                return;

            var recentReportCount = (PlannedWorkerCount >> 2).Clamp(1, RecentReports.Count);
            for (var i = 0; i < recentReportCount; i++)
                minQueueTime = TimeSpanExt.Min(minQueueTime, RecentReports[i]);
        }
        var delta = WorkerPolicy.GetWorkerCountDelta(minQueueTime);
        _ = AddOrRemoveWorkers(delta);
    }

    protected async ValueTask AddOrRemoveWorkers(int delta, bool ignoreCooldown = false)
    {
        if (delta == 0)
            return;

        var wp = WorkerPolicy;
        lock (Lock) {
            var now = CpuTimestamp.Now;
            if (CooldownEndsAt > now && !ignoreCooldown)
                return;

            CooldownEndsAt = now + WorkerPolicy.Cooldown;
            var oldPlannedWorkerCount = PlannedWorkerCount;
            PlannedWorkerCount = (oldPlannedWorkerCount + delta).Clamp(wp.MinWorkerCount, wp.MaxWorkerCount);
            if (StopToken.IsCancellationRequested) {
                PlannedWorkerCount = 0;
                Queue.Writer.TryComplete();
                return;
            }

            delta = PlannedWorkerCount - oldPlannedWorkerCount;
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
            int workerCount;
            using var flowSuppressor = ExecutionContextExt.SuppressFlow();
            lock (Lock) {
                WorkerCollectorTask ??= Task.Run(RunWorkerCollector, CancellationToken.None);
                for (; delta > 0; delta--) {
                    var workerTask = Task.Run(RunWorker, CancellationToken.None);
                    Workers.Add(workerTask);
                    _ = workerTask.ContinueWith(static (task, state) => {
                        var self = (BatchProcessor<T, TResult>)state!;
                        lock (self.Lock)
                            self.Workers.Remove(task);
                    }, this, TaskScheduler.Default);
                }
                workerCount = Workers.Count;
            }
            if (workerCount >= wp.MaxWorkerCount)
                Log?.LogWarning("{BatchProcessor}: High worker count: {WorkerCount}",
                    GetType().GetName(), workerCount);
        }
    }

    protected virtual async Task RunWorkerCollector()
    {
        while (!StopToken.IsCancellationRequested) {
            var wp = WorkerPolicy;
            var longCycle = wp.CollectorCycle;
            var shortCycle = wp.Cooldown + TimeSpan.FromMilliseconds(50);
            var nextCycle = longCycle;
            while (!StopToken.IsCancellationRequested)
                try {
                    await Task.Delay(nextCycle, StopToken).ConfigureAwait(false);

                    // Measure queue time
                    var item = new Item(default!, StopToken) { IsMeasureOnlyItem = true };
                    await Queue.Writer.WriteAsync(item).ConfigureAwait(false);
                    await item.ResultTask.ConfigureAwait(false);
                    var minQueueTime = item.DequeuedAt - item.QueuedAt;

                    // Adjust worker count
                    var delta = WorkerPolicy.GetWorkerCountDelta(minQueueTime);
                    await AddOrRemoveWorkers(delta, true).ConfigureAwait(false);

                    // Decide on how quickly to run the next cycle
                    nextCycle = delta < 0 ? shortCycle : longCycle;
                    if (!ReferenceEquals(WorkerPolicy, wp))
                        break;
                }
                catch {
                    nextCycle = shortCycle;
                }
        }
    }

    protected virtual async Task RunWorker()
    {
        var reader = Queue.Reader;
        var batch = new List<Item>(BatchSize);
        var minQueueTime = TimeSpan.MaxValue;
        var reportCounter = 0;
        try {
            while (await reader.WaitToReadAsync().ConfigureAwait(false)) {
                while (reader.TryRead(out var item)) {
                    if (ReferenceEquals(item, Item.WorkerKiller)) {
                        await ProcessBatch(batch).ConfigureAwait(false);
                        return;
                    }
                    item.DequeuedAt = CpuTimestamp.Now;
                    if (item.IsMeasureOnlyItem) {
                        item.SetResult(default(TResult)!);
                        continue;
                    }
                    var queueTime = item.DequeuedAt - item.QueuedAt;
                    minQueueTime = TimeSpanExt.Min(queueTime, minQueueTime);
                    if (++reportCounter >= BatchSize) {
                        ProcessWorkerReport(minQueueTime);
                        reportCounter = 0;
                        minQueueTime = TimeSpan.MaxValue;
                    }
                    batch.Add(item);
                    if (batch.Count >= BatchSize)
                        await ProcessBatch(batch).ConfigureAwait(false);
                }
                await ProcessBatch(batch).ConfigureAwait(false);
            }
        }
        catch (Exception e) {
            Log?.LogError(e, "{BatchProcessor}: Worker failed", GetType().GetName());
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
        foreach (var item in batch) {
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
        private static readonly TaskCompletionSource<TResult> WorkerKillSource
            = TaskCompletionSourceExt.New<TResult>()
                .WithException(Errors.InternalError("Something is off: you should never see the WorkerKiller item in batches."));

        public static readonly Item WorkerKiller = new(default!, WorkerKillSource, default);

        public readonly T Input = input;
        public readonly TaskCompletionSource<TResult> ResultSource = resultSource;
        public readonly CancellationToken CancellationToken = cancellationToken;
        public readonly CpuTimestamp QueuedAt = CpuTimestamp.Now;
        public CpuTimestamp DequeuedAt;
        public Task<TResult> ResultTask => ResultSource.Task;
        public bool IsMeasureOnlyItem;

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
