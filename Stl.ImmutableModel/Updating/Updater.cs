using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Updating
{
    public interface IUpdater
    {
        INode Model { get; }
        IUpdatableIndex Index { get; }
        Task<IUpdateInfo> UpdateAsync(
            Func<IUpdatableIndex, (IUpdatableIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
        event Action<IUpdateInfo> Updated;
    }
    
    public interface IUpdater<TModel> : IUpdater
        where TModel : class, INode
    {
        new TModel Model { get; }
        new IUpdatableIndex<TModel> Index { get; }
        Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdatableIndex<TModel>, (IUpdatableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
        new event Action<UpdateInfo<TModel>> Updated;
    }
    
    [Serializable]
    public abstract class UpdaterBase<TModel> : IUpdater<TModel>
        where TModel : class, INode
    {
        protected volatile IUpdatableIndex<TModel> IndexField;

        IUpdatableIndex IUpdater.Index => IndexField;
        public IUpdatableIndex<TModel> Index => IndexField;

        INode IUpdater.Model => Model;
        [JsonIgnore] public TModel Model => Index.Model;

        event Action<IUpdateInfo> IUpdater.Updated {
            add => Updated += value;
            remove => Updated -= value;
        }
        public event Action<UpdateInfo<TModel>>? Updated;

        [JsonConstructor]
        protected UpdaterBase(IUpdatableIndex<TModel> index) => IndexField = index;

        public async Task<IUpdateInfo> UpdateAsync(
            Func<IUpdatableIndex, (IUpdatableIndex NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = await UpdateAsync(source => {
                var r = updater.Invoke(source);
                return ((IUpdatableIndex<TModel>) r.NewIndex, r.ChangeSet);
            }, cancellationToken);
            return result;
        }

        public abstract Task<UpdateInfo<TModel>> UpdateAsync(
            Func<IUpdatableIndex<TModel>, (IUpdatableIndex<TModel> NewIndex, ChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);

        protected virtual void OnUpdated(UpdateInfo<TModel> updateInfo)
            => Updated?.Invoke(updateInfo);
    }
}
