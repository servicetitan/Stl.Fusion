using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Updating
{
    public interface IModelUpdateInfo
    {
        ModelChangeSet ChangeSet { get; }
        IUpdatableIndex OldIndex { get; }
        IUpdatableIndex NewIndex { get; }
        INode OldModel { get; }
        INode NewModel { get; }
    }

    public interface IModelUpdateInfo<out TModel> : IModelUpdateInfo
        where TModel : class, INode
    {
        new IUpdatableIndex<TModel> OldIndex { get; }
        new IUpdatableIndex<TModel> NewIndex { get; }
        new TModel OldModel { get; }
        new TModel NewModel { get; }
    }

    [Serializable]
    public class ModelUpdateInfo<TModel> : IModelUpdateInfo<TModel>
        where TModel : class, INode
    {
        IUpdatableIndex IModelUpdateInfo.OldIndex => OldIndex;
        IUpdatableIndex IModelUpdateInfo.NewIndex => NewIndex;
        INode IModelUpdateInfo.OldModel => OldModel;
        INode IModelUpdateInfo.NewModel => NewModel;

        public ModelChangeSet ChangeSet { get; }
        public IUpdatableIndex<TModel> OldIndex { get; }
        public IUpdatableIndex<TModel> NewIndex { get; }
        [JsonIgnore] public TModel OldModel => OldIndex.Model;
        [JsonIgnore] public TModel NewModel => NewIndex.Model;

        [JsonConstructor]
        public ModelUpdateInfo(IUpdatableIndex<TModel> oldIndex, IUpdatableIndex<TModel> newIndex, ModelChangeSet changeSet) 
        {
            ChangeSet = changeSet;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
