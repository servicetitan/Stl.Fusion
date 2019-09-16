using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating 
{
    [Serializable]
    public class QueueingUpdater<TModel> : UpdaterBase<TModel>, IAsyncDisposable
        where TModel : class, INode
    {
        protected AsyncChannel<(
            Func<IUpdateableIndex<TModel>, (IUpdateableIndex<TModel> NewIndex, ChangeSet ChangeSet)> Updater,
            CancellationToken CancellationToken,
            TaskCompletionSource<UpdateInfo<TModel>> Result)> UpdateQueue { get; set; } = 
            new AsyncChannel<(
                Func<IUpdateableIndex<TModel>, (IUpdateableIndex<TModel> NewIndex, ChangeSet ChangeSet)> Updater, 
                CancellationToken CancellationToken,
                TaskCompletionSource<UpdateInfo<TModel>> Result)>(64);
        protected Task QueueProcessorTask { get; set; }

        [JsonConstructor]
        public QueueingUpdater(IUpdateableIndex<TModel> index) : base(index) 
            => QueueProcessorTask = Task.Run(QueueProcessor);

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
                var updateInfo = new UpdateInfo<TModel>(oldIndex, newIndex, changeSet);
                OnUpdated(updateInfo);
                result.SetResult(updateInfo);
            }
        }

        public override async Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdateableIndex<TModel>, (IUpdateableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = new TaskCompletionSource<UpdateInfo<TModel>>();
            await UpdateQueue
                .PutAsync((updater, cancellationToken, result), cancellationToken)
                .ConfigureAwait(false);
            return await result.Task;
        }
    }

    public static class QueuingUpdater
    {
        public static QueueingUpdater<TModel> New<TModel>(IUpdateableIndex<TModel> index)
            where TModel : class, INode 
            => new QueueingUpdater<TModel>(index);
    }
}
