using System;
using Newtonsoft.Json;
using Stl.Comparison;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel.Indexing
{
    public interface IUpdateableIndex : IIndex
    {
        (IUpdateableIndex Index, ChangeSet ChangeSet) BaseUpdate(INode source, INode target);
    }

    public interface IUpdateableIndex<out TModel> : IIndex<TModel>, IUpdateableIndex
        where TModel : class, INode
    { }

    [Serializable]
    public class UpdatableIndex : Index, IUpdateableIndex
    {
        public static UpdatableIndex<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new UpdatableIndex<TModel>(model);

        [JsonConstructor]
        public UpdatableIndex(INode untypedModel) : base(untypedModel) { }

        public virtual (IUpdateableIndex Index, ChangeSet ChangeSet) BaseUpdate(
            INode source, INode target)
        {
            if (source == target)
                return (this, ChangeSet.Empty);

            if (source.Key != target.Key)
                throw Errors.InvalidUpdateKeyMismatch();
            
            var clone = (UpdatableIndex) MemberwiseClone();
            var changeSet = new ChangeSet();
            clone.UpdateNode(source, target, ref changeSet);
            return (clone, changeSet);
        }

        protected virtual void UpdateNode(INode source, INode target, ref ChangeSet changeSet)
        {
            SymbolPath? path = this.GetPath(source);
            CompareAndUpdateNode(path, source, target, ref changeSet);

            var tail = path.Tail;
            path = path.Head;
            while (path != null) {
                var sourceParent = this.GetNode(path);
                var targetParent = sourceParent.DualWith(tail, Option.Some((object?) target));
                ReplaceNode(path, sourceParent, targetParent, ref changeSet);
                source = sourceParent;
                target = targetParent;
                tail = path.Tail;
                path = path.Head;
            }
            UntypedModel = target;
        }

        private void CompareAndUpdateNode(SymbolPath path, INode source, INode target, ref ChangeSet changeSet)
        {
            if (source == target)
                return;

            var sPairs = source.DualGetItems().ToDictionary();
            var tPairs = target.DualGetItems().ToDictionary();
            var c = DictionaryComparison.New(sPairs, tPairs);
            if (c.AreEqual)
                return;

            var changeKind = ChangeKind.SubtreeChanged;
            foreach (var (key, item) in c.LeftOnly) {
                if (item is INode n)
                    RemoveNode(path + key, n, ref changeSet);
            }
            foreach (var (key, item) in c.RightOnly) {
                if (item is INode n)
                    AddNode(path + key, n, ref changeSet);
            }
            foreach (var (key, sItem, tItem) in c.SharedUnequal) {
                if (sItem is INode sn) {
                    if (tItem is INode tn)
                        CompareAndUpdateNode(path + key, sn, tn, ref changeSet);
                    else
                        RemoveNode(path + key, sn, ref changeSet);
                }
                else {
                    if (tItem is INode tn)
                        AddNode(path + key, tn, ref changeSet);
                    else
                        changeKind |= ChangeKind.Changed;
                }
            }
            ReplaceNode(path, source, target, ref changeSet, changeKind);
        }
    }

    [Serializable]
    public class UpdatableIndex<TModel> : UpdatableIndex, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        [JsonIgnore] public TModel Model => (TModel) UntypedModel;

        [JsonConstructor]
        public UpdatableIndex(TModel untypedModel) : base(untypedModel) { }
    }

}
