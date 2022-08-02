using Stl.OS;

namespace Stl.Async;

public abstract class BatchProcessorBase<TIn, TOut> : WorkerBase
{
    public const int DefaultCapacity = 4096;
    public int ConcurrencyLevel { get; set; } = HardwareInfo.GetProcessorCountFactor();
    public int MaxBatchSize { get; set; } = 256;
    public Func<CancellationToken, Task>? BatchingDelayTaskFactory { get; set; }
    protected Channel<BatchItem<TIn, TOut>> Queue { get; }

    protected BatchProcessorBase(int capacity = DefaultCapacity)
        : this(new BoundedChannelOptions(capacity)) { }
    protected BatchProcessorBase(BoundedChannelOptions options)
        : this(Channel.CreateBounded<BatchItem<TIn, TOut>>(options)) { }
    protected BatchProcessorBase(Channel<BatchItem<TIn, TOut>> queue)
        => Queue = queue;

    public async Task<TOut> Process(TIn input, CancellationToken cancellationToken = default)
    {
        Start();
        var outputTask = TaskSource.New<TOut>(false).Task;
        var batchItem = new BatchItem<TIn, TOut>(input, cancellationToken, outputTask);
        await Queue.Writer.WriteAsync(batchItem, cancellationToken).ConfigureAwait(false);
        return await outputTask.ConfigureAwait(false);
    }

    protected override Task RunInternal(CancellationToken cancellationToken)
    {
        var readLock = Queue;
        var concurrencyLevel = ConcurrencyLevel;
        var maxBatchSize = MaxBatchSize;

        async Task Worker()
        {
            var reader = Queue.Reader;
            var batch = new List<BatchItem<TIn, TOut>>(maxBatchSize);
            var delayTask = (Task?) null;
            var delayCts = (CancellationTokenSource?) null;
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    lock (readLock) {
                        while (batch.Count < maxBatchSize && reader.TryRead(out var item))
                            batch.Add(item);
                    }

                    // Do we have any items batched?
                    if (batch.Count == 0) {
                        // Nope, so the only option is to wait for the next item
                        await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    // Yes, we have some items; lets try to start delayTask first
                    if (delayTask == null && BatchingDelayTaskFactory != null && batch.Count < maxBatchSize) {
                        // Very first item, so let's start the new delayTask
                        delayCts.CancelAndDisposeSilently();
                        delayCts = cancellationToken.CreateLinkedTokenSource();
                        delayTask = BatchingDelayTaskFactory.Invoke(delayCts.Token);
                    }

                    if (delayTask != null) {
                        // Here we know there are items + there is delayTask,
                        // so we must try to wait for more while it is running
                        var waitToReadTask = reader.WaitToReadAsync(delayCts!.Token).AsTask();
                        var completedTask = await Task.WhenAny(delayTask, waitToReadTask).ConfigureAwait(false);
                        if (completedTask == waitToReadTask) {
                            if (!await waitToReadTask.ConfigureAwait(false))
                                break; // No more items to read
                            continue; // There are items ready to be read
                        }

                        // There are batched items & delayTask is either absent or completed,
                        // so we need to process the batch
                        await delayTask.ConfigureAwait(false);
                        delayTask = null;
                        delayCts.CancelAndDisposeSilently();
                        delayCts = null;
                    }

                    try {
                        await ProcessBatch(batch, cancellationToken).ConfigureAwait(false);
                    }
                    finally {
                        batch.Clear();
                    }
                }
            }
            finally {
                delayCts.CancelAndDisposeSilently();
            }
        }

        var workerTasks = new Task[concurrencyLevel];
        for (var i = 0; i < concurrencyLevel; i++)
            workerTasks[i] = Task.Run(Worker, cancellationToken);
        return Task.WhenAll(workerTasks);
    }

    protected abstract Task ProcessBatch(List<BatchItem<TIn, TOut>> batch, CancellationToken cancellationToken);
}

public class BatchProcessor<TIn, TOut> : BatchProcessorBase<TIn, TOut>
{
    public Func<List<BatchItem<TIn, TOut>>, CancellationToken, Task> Implementation { get; set; } =
        (_, _) => throw new NotSupportedException("Set the delegate property to make it work.");

    public BatchProcessor(int capacity = DefaultCapacity) : base(capacity) { }
    public BatchProcessor(BoundedChannelOptions options) : base(options) { }
    public BatchProcessor(Channel<BatchItem<TIn, TOut>> queue) : base(queue) { }

    protected override async Task ProcessBatch(List<BatchItem<TIn, TOut>> batch, CancellationToken cancellationToken)
    {
        try {
            await Implementation.Invoke(batch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            if (!cancellationToken.IsCancellationRequested)
                cancellationToken = new CancellationToken(canceled: true);
            foreach (var item in batch)
                item.TryCancel(cancellationToken);
            throw;
        }
        catch (Exception e) {
            var result = Result.Error<TOut>(e);
            foreach (var item in batch)
                item.SetResult(result, cancellationToken);
            throw;
        }
    }
}
