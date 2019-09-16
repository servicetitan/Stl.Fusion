using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    public interface IUpdater
    {
        INode UntypedModel { get; }
        IUpdateableIndex UntypedIndex { get; }
        Task<UpdateInfo> UpdateAsync(
            Func<IUpdateableIndex, (IUpdateableIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
        event Action<UpdateInfo> Updated;
    }
    
    public interface IUpdater<TIndex, TModel> : IUpdater
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        TModel Model { get; }
        TIndex Index { get; }
        Task<UpdateInfo<TIndex, TModel>> UpdateAsync(
            Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
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
        public event Action<UpdateInfo<TIndex, TModel>>? Updated;

        [JsonConstructor]
        protected UpdaterBase(TIndex index) => _index = index;

        public async Task<UpdateInfo> UpdateAsync(
            Func<IUpdateableIndex, (IUpdateableIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = await UpdateAsync(source => {
                var r = updater.Invoke(source);
                return ((TIndex) r.NewIndex, r.ChangeSet);
            }, cancellationToken);
            return result;
        }

        public abstract Task<UpdateInfo<TIndex, TModel>> UpdateAsync(
            Func<TIndex, (TIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);

        protected virtual void OnUpdated(UpdateInfo<TIndex, TModel> updateInfo)
            => Updated?.Invoke(updateInfo);
    }
}
