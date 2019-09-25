using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.Async;

namespace Stl.ImmutableModel.Updating
{
    public interface IModelUpdater : IModelProvider, IAsyncDisposable
    {
        Task<IModelUpdateInfo> UpdateAsync(
            Func<IUpdatableIndex, (IUpdatableIndex NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
    }
    
    public interface IModelUpdater<TModel> : IModelUpdater, IModelProvider<TModel>
        where TModel : class, INode
    {
        Task<ModelUpdateInfo<TModel>> UpdateAsync(
            Func<IUpdatableIndex<TModel>, (IUpdatableIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);
    }
    
    [Serializable]
    public abstract class ModelUpdaterBase<TModel> : AsyncDisposableBase, IModelUpdater<TModel>
        where TModel : class, INode
    {
        protected volatile IUpdatableIndex<TModel> IndexField;
        protected IModelChangeNotify<TModel> ChangeTrackerNotifier;

        IUpdatableIndex IModelProvider.Index => IndexField;
        IModelChangeTracker IModelProvider.ChangeTracker => ChangeTracker;
        INode IModelProvider.Model => Model;

        public IUpdatableIndex<TModel> Index => IndexField;
        [JsonIgnore] public IModelChangeTracker<TModel> ChangeTracker { get; }
        [JsonIgnore] public TModel Model => Index.Model;

        protected ModelUpdaterBase(IUpdatableIndex<TModel> index) 
            : this(index, new ModelChangeTracker<TModel>())
        { }

        protected ModelUpdaterBase(IUpdatableIndex<TModel> index, IModelChangeTracker<TModel> changeTracker)
        {
            IndexField = index;
            ChangeTracker = changeTracker;
            ChangeTrackerNotifier = (IModelChangeNotify<TModel>) changeTracker;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            ChangeTracker?.Dispose();
            return ValueTaskEx.CompletedTask;
        }

        public Type GetModelType() => typeof(TModel);

        public async Task<IModelUpdateInfo> UpdateAsync(
            Func<IUpdatableIndex, (IUpdatableIndex NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default)
        {
            var result = await UpdateAsync(source => {
                var r = updater.Invoke(source);
                return ((IUpdatableIndex<TModel>) r.NewIndex, r.ChangeSet);
            }, cancellationToken);
            return result;
        }

        public abstract Task<ModelUpdateInfo<TModel>> UpdateAsync(
            Func<IUpdatableIndex<TModel>, (IUpdatableIndex<TModel> NewIndex, ModelChangeSet ChangeSet)> updater,
            CancellationToken cancellationToken = default);

        protected virtual void OnUpdated(ModelUpdateInfo<TModel> updateInfo)
            => ChangeTrackerNotifier.OnModelUpdated(updateInfo);
    }
}
