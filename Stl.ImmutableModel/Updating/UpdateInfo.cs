using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    [Serializable]
    public class UpdateInfo
    {
        public IUpdatableIndex UntypedOldIndex { get; }
        public IUpdatableIndex UntypedNewIndex { get; }
        [JsonIgnore] public INode UntypedOldModel => UntypedOldIndex.UntypedModel;
        [JsonIgnore] public INode UntypedNewModel => UntypedNewIndex.UntypedModel;
        public ChangeSet ChangeSet { get; }

        [JsonConstructor]
        public UpdateInfo(
            IUpdatableIndex untypedOldIndex, 
            IUpdatableIndex untypedNewIndex, 
            ChangeSet changeSet)
        {
            UntypedOldIndex = untypedOldIndex;
            UntypedNewIndex = untypedNewIndex;
            ChangeSet = changeSet;
        }
    }

    public class UpdateInfo<TModel> : UpdateInfo
        where TModel : class, INode
    {
        [JsonIgnore] public IUpdatableIndex<TModel> OldIndex => (IUpdatableIndex<TModel>) UntypedOldIndex;
        [JsonIgnore] public IUpdatableIndex<TModel> NewIndex => (IUpdatableIndex<TModel>) UntypedNewIndex;
        [JsonIgnore] public TModel OldModel => (TModel) UntypedOldModel;
        [JsonIgnore] public TModel NewModel => (TModel) UntypedNewModel;

        [JsonConstructor]
        public UpdateInfo(IUpdatableIndex<TModel> oldIndex, IUpdatableIndex<TModel> newIndex, ChangeSet changeSet)  
            : base(oldIndex, newIndex, changeSet) { }
    }
}
