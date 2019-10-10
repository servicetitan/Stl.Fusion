using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    public interface IModelUpdateInfo
    {
        ModelChangeSet ChangeSet { get; }
        IModelIndex OldModelIndex { get; }
        IModelIndex NewModelIndex { get; }
        INode OldModel { get; }
        INode NewModel { get; }
    }

    public interface IModelUpdateInfo<out TModel> : IModelUpdateInfo
        where TModel : class, INode
    {
        new IModelIndex<TModel> OldModelIndex { get; }
        new IModelIndex<TModel> NewModelIndex { get; }
        new TModel OldModel { get; }
        new TModel NewModel { get; }
    }

    [Serializable]
    public class ModelUpdateInfo<TModel> : IModelUpdateInfo<TModel>
        where TModel : class, INode
    {
        IModelIndex IModelUpdateInfo.OldModelIndex => OldModelIndex;
        IModelIndex IModelUpdateInfo.NewModelIndex => NewModelIndex;
        INode IModelUpdateInfo.OldModel => OldModel;
        INode IModelUpdateInfo.NewModel => NewModel;

        public ModelChangeSet ChangeSet { get; }
        public IModelIndex<TModel> OldModelIndex { get; }
        public IModelIndex<TModel> NewModelIndex { get; }
        [JsonIgnore] public TModel OldModel => OldModelIndex.Model;
        [JsonIgnore] public TModel NewModel => NewModelIndex.Model;

        [JsonConstructor]
        public ModelUpdateInfo(IModelIndex<TModel> oldModelIndex, IModelIndex<TModel> newModelIndex, ModelChangeSet changeSet) 
        {
            ChangeSet = changeSet;
            OldModelIndex = oldModelIndex;
            NewModelIndex = newModelIndex;
        }
    }
}
