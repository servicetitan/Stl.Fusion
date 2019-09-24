using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Updating
{
    public interface IUpdateInfo
    {
        ChangeSet ChangeSet { get; }
        IUpdatableIndex OldIndex { get; }
        IUpdatableIndex NewIndex { get; }
        INode OldModel { get; }
        INode NewModel { get; }
    }

    public interface IUpdateInfo<out TModel> : IUpdateInfo
        where TModel : class, INode
    {
        new IUpdatableIndex<TModel> OldIndex { get; }
        new IUpdatableIndex<TModel> NewIndex { get; }
        new TModel OldModel { get; }
        new TModel NewModel { get; }
    }

    [Serializable]
    public class UpdateInfo<TModel> : IUpdateInfo<TModel>
        where TModel : class, INode
    {
        IUpdatableIndex IUpdateInfo.OldIndex => OldIndex;
        IUpdatableIndex IUpdateInfo.NewIndex => NewIndex;
        INode IUpdateInfo.OldModel => OldModel;
        INode IUpdateInfo.NewModel => NewModel;

        public ChangeSet ChangeSet { get; }
        public IUpdatableIndex<TModel> OldIndex { get; }
        public IUpdatableIndex<TModel> NewIndex { get; }
        [JsonIgnore] public TModel OldModel => OldIndex.Model;
        [JsonIgnore] public TModel NewModel => NewIndex.Model;

        [JsonConstructor]
        public UpdateInfo(IUpdatableIndex<TModel> oldIndex, IUpdatableIndex<TModel> newIndex, ChangeSet changeSet) 
        {
            ChangeSet = changeSet;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
