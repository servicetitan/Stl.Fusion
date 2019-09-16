using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    
    public interface IUpdater<TModel> : IUpdater
        where TModel : class, INode
    {
        TModel Model { get; }
        IUpdateableIndex<TModel> Index { get; }
        Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdateableIndex<TModel>, (IUpdateableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
        new event Action<UpdateInfo<TModel>> Updated;
    }
    
    [Serializable]
    public abstract class UpdaterBase<TModel> : IUpdater<TModel>
        where TModel : class, INode
    {
        protected volatile IUpdateableIndex<TModel> _index;

        IUpdateableIndex IUpdater.UntypedIndex => _index;
        public IUpdateableIndex<TModel> Index => _index;

        INode IUpdater.UntypedModel => Model;
        [JsonIgnore] public TModel Model => Index.Model;

        event Action<UpdateInfo> IUpdater.Updated {
            add => Updated += value;
            remove => Updated -= value;
        }
        public event Action<UpdateInfo<TModel>>? Updated;

        [JsonConstructor]
        protected UpdaterBase(IUpdateableIndex<TModel> index) => _index = index;

        public async Task<UpdateInfo> UpdateAsync(
            Func<IUpdateableIndex, (IUpdateableIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = await UpdateAsync(source => {
                var r = updater.Invoke(source);
                return ((IUpdateableIndex<TModel>) r.NewIndex, r.ChangeSet);
            }, cancellationToken);
            return result;
        }

        public abstract Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdateableIndex<TModel>, (IUpdateableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);

        protected virtual void OnUpdated(UpdateInfo<TModel> updateInfo)
            => Updated?.Invoke(updateInfo);
    }
}
