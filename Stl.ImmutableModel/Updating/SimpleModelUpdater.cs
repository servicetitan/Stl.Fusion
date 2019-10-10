using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating 
{
    [Serializable]
    public class SimpleModelUpdater<TModel> : ModelUpdaterBase<TModel>
        where TModel : class, INode
    {
        protected AsyncCounter UpdateCounter { get; } = new AsyncCounter();

        [JsonConstructor]
        public SimpleModelUpdater(IModelIndex<TModel> index) : base(index) { }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            // Await for completion of all pending updates
            await UpdateCounter.DisposeAsync().ConfigureAwait(false);
            // And release the rest of resources
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }

        public override Task<ModelUpdateInfo<TModel>> UpdateAsync(
            Func<IModelIndex<TModel>, (IModelIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            using var _ = UpdateCounter.Use();
            IModelIndex<TModel> oldModelIndex, newModelIndex;
            ModelChangeSet changeSet;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                oldModelIndex = Index;
                (newModelIndex, changeSet) = updater.Invoke(oldModelIndex);
                if (Interlocked.CompareExchange(ref CurrentModelIndex, newModelIndex, oldModelIndex) == oldModelIndex)
                    break;
            }
            var updateInfo = new ModelUpdateInfo<TModel>(oldModelIndex, newModelIndex, changeSet);
            OnUpdated(updateInfo);
            return Task.FromResult(updateInfo);
        }
    }

    public static class SimpleModelUpdater
    {
        public static SimpleModelUpdater<TModel> New<TModel>(IModelIndex<TModel> index)
            where TModel : class, INode 
            => new SimpleModelUpdater<TModel>(index);
    }
}
