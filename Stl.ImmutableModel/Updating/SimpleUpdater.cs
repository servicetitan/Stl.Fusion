using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating 
{
    [Serializable]
    public class SimpleUpdater<TModel> : UpdaterBase<TModel>
        where TModel : class, INode
    {
        [JsonConstructor]
        public SimpleUpdater(IUpdatableIndex<TModel> index) : base(index) { }

        public override Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdatableIndex<TModel>, (IUpdatableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            IUpdatableIndex<TModel> oldIndex, newIndex;
            ChangeSet changeSet;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                oldIndex = Index;
                (newIndex, changeSet) = updater.Invoke(oldIndex);
                if (Interlocked.CompareExchange(ref IndexField, newIndex, oldIndex) == oldIndex)
                    break;
            }
            var updateInfo = new UpdateInfo<TModel>(oldIndex, newIndex, changeSet);
            OnUpdated(updateInfo);
            return Task.FromResult(updateInfo);
        }
    }

    public static class SimpleUpdater
    {
        public static SimpleUpdater<TModel> New<TModel>(IUpdatableIndex<TModel> index)
            where TModel : class, INode 
            => new SimpleUpdater<TModel>(index);
    }
}
