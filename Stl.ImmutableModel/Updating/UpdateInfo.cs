using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel.Updating
{
    [Serializable]
    public abstract class UpdateInfo
    {
        [JsonIgnore] public abstract IUpdatableIndex UntypedOldIndex { get; }
        [JsonIgnore] public abstract IUpdatableIndex UntypedNewIndex { get; }
        [JsonIgnore] public INode UntypedOldModel => UntypedOldIndex.UntypedModel;
        [JsonIgnore] public INode UntypedNewModel => UntypedNewIndex.UntypedModel;
        public ChangeSet ChangeSet { get; }

        protected UpdateInfo(ChangeSet changeSet) => ChangeSet = changeSet;
    }

    public class UpdateInfo<TModel> : UpdateInfo
        where TModel : class, INode
    {
        public IUpdatableIndex<TModel> OldIndex { get; }
        public IUpdatableIndex<TModel> NewIndex { get; }
        [JsonIgnore] public override IUpdatableIndex UntypedOldIndex => OldIndex;
        [JsonIgnore] public override IUpdatableIndex UntypedNewIndex => NewIndex;
        [JsonIgnore] public TModel OldModel => OldIndex.Model;
        [JsonIgnore] public TModel NewModel => NewIndex.Model;

        [JsonConstructor]
        public UpdateInfo(IUpdatableIndex<TModel> oldIndex, IUpdatableIndex<TModel> newIndex, ChangeSet changeSet) 
            : base(changeSet)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }
    }
}
