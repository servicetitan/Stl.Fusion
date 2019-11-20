using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel
{
    // It's intentional these types don't expose IModelUpdater:
    // the providers are supposed to provide the access to
    // read-only models + an API for tracking the changes there,
    // but not the way to update the models.

    public interface IModelProvider
    {
        INode Model { get; }
        IModelIndex Index { get; }
        IModelChangeTracker ChangeTracker { get; }
        Type GetModelType();
    }

    public interface IModelProvider<TModel> : IModelProvider
        where TModel : class, INode
    {
        new TModel Model { get; }
        new IModelIndex<TModel> Index { get; }
        new IModelChangeTracker<TModel> ChangeTracker { get; }
    }

    [Serializable]
    public class ModelProvider<TModel> : IModelProvider<TModel>
        where TModel : class, INode
    {
        INode IModelProvider.Model => Updater.Model;
        IModelIndex IModelProvider.Index => Updater.Index;
        IModelChangeTracker IModelProvider.ChangeTracker => Updater.ChangeTracker;

        // TODO: Turn this into a protected field, but keep JSON serialization working 
        public IModelUpdater<TModel> Updater { get; }
        [JsonIgnore]
        public TModel Model => Updater.Model;
        [JsonIgnore]
        public IModelIndex<TModel> Index => Updater.Index;
        [JsonIgnore]
        public IModelChangeTracker<TModel> ChangeTracker => Updater.ChangeTracker;

        [JsonConstructor]
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
