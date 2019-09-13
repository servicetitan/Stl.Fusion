using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Updaters 
{
    [Serializable]
    public class SimpleUpdater<TIndex, TModel> : UpdaterBase<TIndex, TModel>
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        [JsonConstructor]
        public SimpleUpdater(TIndex index) : base(index) { }

        public override Task<UpdateInfo<TIndex, TModel>> UpdateAsync(
            Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            TIndex oldIndex, newIndex;
            ChangeSet changeSet;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                oldIndex = Index;
                (newIndex, changeSet) = updater.Invoke(oldIndex);
                if (Interlocked.CompareExchange(ref _index, newIndex, oldIndex) == oldIndex)
                    break;
            }
            var updateInfo = new UpdateInfo<TIndex, TModel>(oldIndex, newIndex, changeSet);
            OnUpdated(updateInfo);
            return Task.FromResult(updateInfo);
        }
    }

    public static class SimpleUpdater
    {
        public static SimpleUpdater<TIndex, TModel> New<TIndex, TModel>(TIndex index, TModel model)
            where TIndex : class, IUpdateableIndex<TModel>
            where TModel : class, INode
        {
            if (model != index.Model)
                // "model" argument is here solely to let type inference work
                throw new ArgumentOutOfRangeException(nameof(model));
            return new SimpleUpdater<TIndex, TModel>(index);
        }
    }
}
