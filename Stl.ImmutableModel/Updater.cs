using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel
{
    public interface IUpdater
    {
        INode UntypedModel { get; }
        IUpdateableIndex UntypedIndex { get; }
        UpdateInfo Update(Func<IUpdateableIndex, (IUpdateableIndex NewIndex, ChangeSet ChangeSet)> updater);
        event Action<UpdateInfo> Updated;
    }
    
    public interface IUpdater<TIndex, TModel> : IUpdater
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        TModel Model { get; }
        TIndex Index { get; }
        new UpdateInfo<TIndex, TModel> Update(Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater);
        new event Action<UpdateInfo<TIndex, TModel>> Updated;
    }
    
    [Serializable]
    public abstract class UpdaterBase<TIndex, TModel> : IUpdater<TIndex, TModel>
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        protected volatile TIndex _index;

        [JsonIgnore] public IUpdateableIndex UntypedIndex => _index;
        public TIndex Index => _index;

        [JsonIgnore] public INode UntypedModel => Model;
        [JsonIgnore] public TModel Model => Index.Model;

        event Action<UpdateInfo> IUpdater.Updated {
            add => Updated += value;
            remove => Updated -= value;
        }
        public event Action<UpdateInfo<TIndex, TModel>> Updated;

        [JsonConstructor]
        public UpdaterBase(TIndex index) => _index = index;

        public UpdateInfo Update(Func<IUpdateableIndex, (IUpdateableIndex NewIndex, ChangeSet ChangeSet)> updater)
            => Update(source => {
                var result = updater.Invoke(source);
                return ((TIndex) result.NewIndex, result.ChangeSet);
            });

        public abstract UpdateInfo<TIndex, TModel> Update(Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater);

        protected virtual void OnUpdated(UpdateInfo<TIndex, TModel> updateInfo)
            => Updated?.Invoke(updateInfo);
    }
}
