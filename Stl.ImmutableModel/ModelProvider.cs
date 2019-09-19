using System;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel
{
    // It's intentional these types don't expose IUpdater:
    // the providers are supposed to provide the access to
    // read-only models + an API for tracking the changes there,
    // but not the way to update the models.

    public interface IModelProvider : IDisposable
    {
        INode UntypedModel { get; }
        IUpdatableIndex UntypedIndex { get; }
        IChangeTracker UntypedChangeTracker { get; }
    }

    public interface IModelProvider<TModel> : IModelProvider
        where TModel : class, INode
    {
        TModel Model { get; }
        IUpdatableIndex<TModel> Index { get; }
        IChangeTracker<TModel> ChangeTracker { get; }
    }

    public class ModelProvider<TModel> : IModelProvider<TModel>
        where TModel : class, INode
    {
        IChangeTracker IModelProvider.UntypedChangeTracker => ChangeTracker;
        IUpdatableIndex IModelProvider.UntypedIndex => Index;
        INode IModelProvider.UntypedModel => Model;

        public bool OwnsTracker { get; private set; }
        public IChangeTracker<TModel> ChangeTracker { get; }
        public IUpdatableIndex<TModel> Index => Updater.Index;
        public TModel Model => Index.Model;
        protected IUpdater<TModel> Updater { get; }

        public ModelProvider(IUpdater<TModel> updater)
        {
            OwnsTracker = true;
            ChangeTracker = new ChangeTracker<TModel>(updater);
            Updater = updater;
        }

        public ModelProvider(IChangeTracker<TModel> changeTracker, bool ownsTracker = true)
        {
            OwnsTracker = ownsTracker;
            ChangeTracker = changeTracker;
            Updater = ChangeTracker.Updater;
        }

        public void Dispose()
        {
            if (OwnsTracker) {
                OwnsTracker = false;
                ChangeTracker.Dispose();
            }
        }
    }

    public static class ModelProvider
    {
        public static ModelProvider<TModel> New<TModel>(IUpdater<TModel> updater)
            where TModel : class, INode
            => new ModelProvider<TModel>(updater);

        public static ModelProvider<TModel> New<TModel>(IChangeTracker<TModel> changeTracker, bool ownsTracker = true)
            where TModel : class, INode
            => new ModelProvider<TModel>(changeTracker, ownsTracker);
    }
}
