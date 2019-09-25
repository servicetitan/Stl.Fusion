using System;
using System.Threading.Tasks;
using Stl.Async;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel
{
    // It's intentional these types don't expose IUpdater:
    // the providers are supposed to provide the access to
    // read-only models + an API for tracking the changes there,
    // but not the way to update the models.

    public interface IModelProvider
    {
        INode Model { get; }
        IUpdatableIndex Index { get; }
        IModelChangeTracker ChangeTracker { get; }
        Type GetModelType();
    }

    public interface IModelProvider<TModel> : IModelProvider
        where TModel : class, INode
    {
        new TModel Model { get; }
        new IUpdatableIndex<TModel> Index { get; }
        new IModelChangeTracker<TModel> ChangeTracker { get; }
    }

    public class ModelProvider<TModel> : IModelProvider<TModel>
        where TModel : class, INode
    {
        protected IModelUpdater<TModel> Updater { get; }

        INode IModelProvider.Model => Updater.Model;
        IUpdatableIndex IModelProvider.Index => Updater.Index;
        IModelChangeTracker IModelProvider.ChangeTracker => Updater.ChangeTracker;

        public TModel Model => Updater.Model;
        public IUpdatableIndex<TModel> Index => Updater.Index;
        public IModelChangeTracker<TModel> ChangeTracker => Updater.ChangeTracker;

        public ModelProvider(IModelUpdater<TModel> updater) => Updater = updater;

        public Type GetModelType() => typeof(TModel);
    }

    public static class ModelProvider
    {
        public static ModelProvider<TModel> New<TModel>(IModelUpdater<TModel> modelUpdater)
            where TModel : class, INode
            => new ModelProvider<TModel>(modelUpdater);
    }
}
