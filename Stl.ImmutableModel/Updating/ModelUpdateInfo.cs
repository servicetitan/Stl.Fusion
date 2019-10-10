using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    public interface IModelUpdateInfo
    {
        ModelChangeSet ChangeSet { get; }
        IIndex OldIndex { get; }
        IIndex NewIndex { get; }
        INode OldModel { get; }
        INode NewModel { get; }
    }

    public interface IModelUpdateInfo<out TModel> : IModelUpdateInfo
        where TModel : class, INode
    {
        new IIndex<TModel> OldIndex { get; }
        new IIndex<TModel> NewIndex { get; }
        new TModel OldModel { get; }
        new TModel NewModel { get; }
    }

    [Serializable]
    public class ModelUpdateInfo<TModel> : IModelUpdateInfo<TModel>
        where TModel : class, INode
    {
        IIndex IModelUpdateInfo.OldIndex => OldIndex;
        IIndex IModelUpdateInfo.NewIndex => NewIndex;
        INode IModelUpdateInfo.OldModel => OldModel;
        INode IModelUpdateInfo.NewModel => NewModel;

        public ModelChangeSet ChangeSet { get; }
        public IIndex<TModel> OldIndex { get; }
        public IIndex<TModel> NewIndex { get; }
        [JsonIgnore] public TModel OldModel => OldIndex.Model;
        [JsonIgnore] public TModel NewModel => NewIndex.Model;

        [JsonConstructor]
        public ModelUpdateInfo(IIndex<TModel> oldIndex, IIndex<TModel> newIndex, ModelChangeSet changeSet) 
        {
            ChangeSet = changeSet;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
