using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating 
{
    [Serializable]
    public class QueuingModelUpdater<TModel> : ModelUpdaterBase<TModel>
        where TModel : class, INode
    {
        protected AsyncChannel<(
            Func<IModelIndex<TModel>, (IModelIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> Updater,
            CancellationToken CancellationToken,
            TaskCompletionSource<ModelUpdateInfo<TModel>> Result)> UpdateQueue { get; set; } = 
            new AsyncChannel<(
                Func<IModelIndex<TModel>, (IModelIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> Updater, 
                CancellationToken CancellationToken,
                TaskCompletionSource<ModelUpdateInfo<TModel>> Result)>(64);
        protected Task QueueProcessorTask { get; set; }

        [JsonConstructor]
        public QueuingModelUpdater(IModelIndex<TModel> index) : base(index) 
            => QueueProcessorTask = Task.Run(QueueProcessor);

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            // Await for completion of all pending updates
            UpdateQueue.CompletePut();
            await QueueProcessorTask.SuppressExceptions().ConfigureAwait(false);
            // And release the rest of resources
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }

        protected virtual async Task QueueProcessor()
        {
            while (true) {
                var (isPulled, (updater, cancellationToken, result)) = await UpdateQueue.PullAsync();
                if (!isPulled)
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
                CurrentModelIndex = newIndex;
                var updateInfo = new ModelUpdateInfo<TModel>(oldIndex, newIndex, changeSet);
                OnUpdated(updateInfo);
                result.SetResult(updateInfo);
            }
        }

        public override async Task<ModelUpdateInfo<TModel>> UpdateAsync(
            Func<IModelIndex<TModel>, (IModelIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = new TaskCompletionSource<ModelUpdateInfo<TModel>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await UpdateQueue
                .PutAsync((updater, cancellationToken, result), cancellationToken)
                .ConfigureAwait(false);
            return await result.Task;
        }
    }

    public static class QueuingModelUpdater
    {
        public static QueuingModelUpdater<TModel> New<TModel>(IModelIndex<TModel> index)
            where TModel : class, INode 
            => new QueuingModelUpdater<TModel>(index);
    }
}
