using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.OS;

namespace Stl.Async
{
    public abstract class AsyncBatchProcessorBase<TIn, TOut> : AsyncProcessBase
    {
        public const int DefaultCapacity = 4096;
        public int ConcurrencyLevel { get; set; } = HardwareInfo.GetProcessorCountPo2Factor();
        public int MaxBatchSize { get; set; } = 256;
        public Func<CancellationToken, Task>? BatchingDelayTaskFactory { get; set; }
        protected Channel<BatchItem<TIn, TOut>> Queue { get; }

        protected AsyncBatchProcessorBase(int capacity = DefaultCapacity)
            : this(new BoundedChannelOptions(capacity)) { }
        protected AsyncBatchProcessorBase(BoundedChannelOptions options)
            : this(Channel.CreateBounded<BatchItem<TIn, TOut>>(options)) { }
        protected AsyncBatchProcessorBase(Channel<BatchItem<TIn, TOut>> queue)
            => Queue = queue;

        public async Task<TOut> ProcessAsync(TIn input, CancellationToken cancellationToken = default)
        {
            RunAsync().Ignore();;
            var outputTask = TaskSource.New<TOut>(false).Task;
            var batchItem = new BatchItem<TIn, TOut>(input, cancellationToken, outputTask);
            await Queue.Writer.WriteAsync(batchItem, cancellationToken).ConfigureAwait(false);
            return await outputTask.ConfigureAwait(false);
        }

        protected override Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var readLock = Queue;
            var concurrencyLevel = ConcurrencyLevel;
            var maxBatchSize = MaxBatchSize;

            async Task WorkerAsync()
            {
                var reader = Queue.Reader;
                var batch = new List<BatchItem<TIn, TOut>>(maxBatchSize);
                while (!cancellationToken.IsCancellationRequested) {
                    lock (readLock) {
                        while (batch.Count < maxBatchSize && reader.TryRead(out var item))
                            batch.Add(item);
                    }
                    if (batch.Count == 0) {
                        await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                        if (BatchingDelayTaskFactory != null)
                            await BatchingDelayTaskFactory(cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    try {
                        await ProcessBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    }
                    finally {
                        batch.Clear();
                    }
                }
            }

            var workerTasks = new Task[concurrencyLevel];
            for (var i = 0; i < concurrencyLevel; i++)
                workerTasks[i] = Task.Run(WorkerAsync, cancellationToken);
            return Task.WhenAll(workerTasks);
        }

        protected abstract Task ProcessBatchAsync(List<BatchItem<TIn, TOut>> batch, CancellationToken cancellationToken);
    }

    public class AsyncBatchProcessor<TIn, TOut> : AsyncBatchProcessorBase<TIn, TOut>
    {
        public Func<List<BatchItem<TIn, TOut>>, CancellationToken, Task> BatchProcessor { get; set; } =
            (batch, cancellationToken) => throw new NotImplementedException();

        public AsyncBatchProcessor(int capacity = DefaultCapacity) : base(capacity) { }
        public AsyncBatchProcessor(BoundedChannelOptions options) : base(options) { }
        public AsyncBatchProcessor(Channel<BatchItem<TIn, TOut>> queue) : base(queue) { }

        protected override async Task ProcessBatchAsync(List<BatchItem<TIn, TOut>> batch, CancellationToken cancellationToken)
        {
            try {
                await BatchProcessor.Invoke(batch, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                if (!cancellationToken.IsCancellationRequested)
                    cancellationToken = new CancellationToken(true);
                foreach (var item in batch)
                    item.TryCancel(cancellationToken);
                throw;
            }
            catch (Exception e) {
                var result = Result.Error<TOut>(e);
                foreach (var item in batch)
                    item.SetResult(result, default);
                throw;
            }
        }
    }
}
