using System;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel
{
    public interface IModelProvider : IDisposable
    {
        INode UntypedModel { get; }
        IUpdateableIndex UntypedIndex { get; }
        IUpdater UntypedUpdater { get; }
        IChangeTracker UntypedChangeTracker { get; }
    }

    public interface IModelProvider<TModel> : IModelProvider
        where TModel : class, INode
    {
        TModel Model { get; }
        IUpdateableIndex<TModel> Index { get; }
        IUpdater<TModel> Updater { get; } 
        IChangeTracker<TModel> ChangeTracker { get; }
    }

    public class ModelProvider<TModel> : IModelProvider<TModel>
        where TModel : class, INode
    {
        IChangeTracker IModelProvider.UntypedChangeTracker => ChangeTracker;
        IUpdater IModelProvider.UntypedUpdater => Updater;
        IUpdateableIndex IModelProvider.UntypedIndex => Index;
        INode IModelProvider.UntypedModel => Model;

        public bool OwnsTracker { get; }
        public IChangeTracker<TModel> ChangeTracker { get; }
        public IUpdater<TModel> Updater { get; }
        public IUpdateableIndex<TModel> Index => Updater.Index;
        public TModel Model => Index.Model;

        public ModelProvider(IChangeTracker<TModel> changeTracker, bool ownsTracker = true)
        {
            OwnsTracker = ownsTracker;
            ChangeTracker = changeTracker;
            Updater = ChangeTracker.Updater;
        }

        public void Dispose()
        {
            if (OwnsTracker)
                ChangeTracker.Dispose();
        }
    }

    public static class ModelProvider
    {
        public static ModelProvider<TModel> New<TModel>(IChangeTracker<TModel> changeTracker, bool ownsTracker = true)
            where TModel : class, INode
            => new ModelProvider<TModel>(changeTracker, ownsTracker);
    }
}
