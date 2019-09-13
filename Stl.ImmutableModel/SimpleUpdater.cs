using System;
using System.Threading;
using Newtonsoft.Json;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public class SimpleUpdater<TIndex, TModel> : UpdaterBase<TIndex, TModel>
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        [JsonConstructor]
        public SimpleUpdater(TIndex index) : base(index) { }

        public override UpdateInfo<TIndex, TModel> Update(Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater)
        {
            TIndex oldIndex, newIndex;
            ChangeSet changeSet;
            while (true) {
                oldIndex = Index;
                (newIndex, changeSet) = updater.Invoke(oldIndex);
                if (Interlocked.CompareExchange(ref _index, newIndex, oldIndex) == oldIndex)
                    break;
            }
            var updateInfo = UpdateInfo.New<TIndex, TModel>(oldIndex, newIndex, changeSet);
            OnUpdated(updateInfo);
            return updateInfo;
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
