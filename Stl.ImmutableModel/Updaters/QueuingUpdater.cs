using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;

namespace Stl.ImmutableModel.Updaters 
{
    [Serializable]
    public class QueueingUpdater<TIndex, TModel> : UpdaterBase<TIndex, TModel>, IAsyncDisposable
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        protected AsyncChannel<(
            Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> Updater,
            CancellationToken CancellationToken,
            TaskCompletionSource<UpdateInfo<TIndex, TModel>> Result)> UpdateQueue { get; set; } = 
            new AsyncChannel<(
                Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> Updater, 
                CancellationToken CancellationToken,
                TaskCompletionSource<UpdateInfo<TIndex, TModel>> Result)>(64);
        protected Task QueueProcessorTask { get; set; }

        [JsonConstructor]
        public QueueingUpdater(TIndex index) : base(index)
        {
            QueueProcessorTask = Task.Run(QueueProcessor);
        }

        public virtual async ValueTask DisposeAsync()
        {
            UpdateQueue.CompletePut();
            await QueueProcessorTask.SuppressExceptions().ConfigureAwait(false);
        }

        protected virtual async Task QueueProcessor()
        {
            while (true) {
                var ((updater, cancellationToken, result), isDequeued) = await UpdateQueue.PullAsync();
                if (!isDequeued)
                    return;
                if (cancellationToken.IsCancellationRequested) {
                    result.SetCanceled();
                    continue;
                }
                var oldIndex = Index;
                var r = updater.InvokeForResult(oldIndex);
                if (r.HasError) {
                    result.SetException(r.Error!);
                    continue;
                }
                var (newIndex, changeSet) = r.Value;
                _index = newIndex;
                var updateInfo = new UpdateInfo<TIndex, TModel>(oldIndex, newIndex, changeSet);
                OnUpdated(updateInfo);
                result.SetResult(updateInfo);
            }
        }

        public override async Task<UpdateInfo<TIndex, TModel>> UpdateAsync(
            Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = new TaskCompletionSource<UpdateInfo<TIndex, TModel>>();
            await UpdateQueue
                .PutAsync((updater, cancellationToken, result), cancellationToken)
                .ConfigureAwait(false);
            return await result.Task;
        }
    }

    public static class QueuingUpdater
    {
        public static QueueingUpdater<TIndex, TModel> New<TIndex, TModel>(TIndex index, TModel model)
            where TIndex : class, IUpdateableIndex<TModel>
            where TModel : class, INode
        {
            if (model != index.Model)
                // "model" argument is here solely to let type inference work
                throw new ArgumentOutOfRangeException(nameof(model));
            return new QueueingUpdater<TIndex, TModel>(index);
        }
    }
}
