using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel
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

        public static UpdateInfo<TIndex, TModel> New<TIndex, TModel>(
            TIndex oldIndex, 
            TIndex newIndex, 
            ChangeSet changeSet) 
            where TIndex : class, IUpdateableIndex<TModel>
            where TModel : class, INode
            => new UpdateInfo<TIndex, TModel>(oldIndex, newIndex, changeSet);
    }

    public class UpdateInfo<TIndex, TModel> : UpdateInfo
        where TIndex : class, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        [JsonIgnore] public TIndex OldIndex => (TIndex) UntypedOldIndex;
        [JsonIgnore] public TIndex NewIndex => (TIndex) UntypedNewIndex;
        [JsonIgnore] public TModel OldModel => (TModel) UntypedOldModel;
        [JsonIgnore] public TModel NewModel => (TModel) UntypedNewModel;

        [JsonConstructor]
        public UpdateInfo(TIndex oldIndex, TIndex newIndex, ChangeSet changeSet)  
            : base(oldIndex, newIndex, changeSet) { }
    }
}
