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
        public SimpleModelUpdater(IIndex<TModel> index) : base(index) { }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            // Await for completion of all pending updates
            await UpdateCounter.DisposeAsync().ConfigureAwait(false);
            // And release the rest of resources
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
        }

        public override Task<ModelUpdateInfo<TModel>> UpdateAsync(
            Func<IIndex<TModel>, (IIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            using var _ = UpdateCounter.Use();
            IIndex<TModel> oldIndex, newIndex;
            ModelChangeSet changeSet;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                oldIndex = Index;
                (newIndex, changeSet) = updater.Invoke(oldIndex);
                if (Interlocked.CompareExchange(ref IndexField, newIndex, oldIndex) == oldIndex)
                    break;
            }
            var updateInfo = new ModelUpdateInfo<TModel>(oldIndex, newIndex, changeSet);
            OnUpdated(updateInfo);
            return Task.FromResult(updateInfo);
        }
    }

    public static class SimpleModelUpdater
    {
        public static SimpleModelUpdater<TModel> New<TModel>(IIndex<TModel> index)
            where TModel : class, INode 
            => new SimpleModelUpdater<TModel>(index);
    }
}
