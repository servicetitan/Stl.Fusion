using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel.Updating
{
    [Serializable]
    public class UpdateInfo
    {
        public IUpdateableIndex UntypedOldIndex { get; }
        public IUpdateableIndex UntypedNewIndex { get; }
        [JsonIgnore] public INode UntypedOldModel => UntypedOldIndex.UntypedModel;
        [JsonIgnore] public INode UntypedNewModel => UntypedNewIndex.UntypedModel;
        public ChangeSet ChangeSet { get; }

        [JsonConstructor]
        public UpdateInfo(
            IUpdateableIndex untypedOldIndex, 
            IUpdateableIndex untypedNewIndex, 
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
        [JsonIgnore] public IUpdateableIndex<TModel> OldIndex => (IUpdateableIndex<TModel>) UntypedOldIndex;
        [JsonIgnore] public IUpdateableIndex<TModel> NewIndex => (IUpdateableIndex<TModel>) UntypedNewIndex;
        [JsonIgnore] public TModel OldModel => (TModel) UntypedOldModel;
        [JsonIgnore] public TModel NewModel => (TModel) UntypedNewModel;

        [JsonConstructor]
        public UpdateInfo(IUpdateableIndex<TModel> oldIndex, IUpdateableIndex<TModel> newIndex, ChangeSet changeSet)  
            : base(oldIndex, newIndex, changeSet) { }
    }
}
