using System;
using Newtonsoft.Json;
using Stl.Comparison;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Internal;
using Index = Stl.ImmutableModel.Indexing.Index;

namespace Stl.ImmutableModel.Updating
{
    public interface IUpdatableIndex : IIndex
    {
        (IUpdatableIndex Index, ModelChangeSet ChangeSet) BaseUpdate(INode source, INode target);
    }

    public interface IUpdatableIndex<out TModel> : IIndex<TModel>, IUpdatableIndex
        where TModel : class, INode
    { }

    [Serializable]
    public abstract class UpdatableIndex : Index, IUpdatableIndex
    {
        public new static UpdatableIndex<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new UpdatableIndex<TModel>(model);

        protected UpdatableIndex() : base() { }

        protected abstract void SetModel(INode model);

        public virtual (IUpdatableIndex Index, ModelChangeSet ChangeSet) BaseUpdate(
            INode source, INode target)
        {
            if (source == target)
                return (this, ModelChangeSet.Empty);

            if (source.LocalKey != target.LocalKey)
                throw Errors.InvalidUpdateKeyMismatch();
            
            var clone = (UpdatableIndex) MemberwiseClone();
            var changeSet = new ModelChangeSet();
            clone.UpdateNode(source, target, ref changeSet);
            return (clone, changeSet);
        }

        protected virtual void UpdateNode(INode source, INode target, ref ModelChangeSet changeSet)
        {
            SymbolList? path = this.GetPath(source);
            CompareAndUpdateNode(path, source, target, ref changeSet);

            var tail = path.Tail;
            path = path.Head;
            while (path != null) {
                var sourceParent = this.GetNodeByPath(path);
                var targetParent = sourceParent.DualWith(tail, Option.Some((object?) target));
                ReplaceNode(path, sourceParent, targetParent, ref changeSet);
                source = sourceParent;
                target = targetParent;
                tail = path.Tail;
                path = path.Head;
            }
            SetModel(target);
        }

        private NodeChangeType CompareAndUpdateNode(SymbolList list, INode source, INode target, ref ModelChangeSet changeSet)
        {
            if (source == target)
                return 0;

            var changeType = (NodeChangeType) 0;
            if (source.GetType() != target.GetType())
                changeType |= NodeChangeType.TypeChanged;

            var sPairs = source.DualGetItems().ToDictionary();
            var tPairs = target.DualGetItems().ToDictionary();
            var c = DictionaryComparison.New(sPairs, tPairs);
            if (c.AreEqual) {
                if (changeType != 0)
                    ReplaceNode(list, source, target, ref changeSet, changeType);
                return changeType;
            }

            foreach (var (key, item) in c.LeftOnly) {
                if (item is INode n) {
                    RemoveNode(list + key, n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (key, item) in c.RightOnly) {
                if (item is INode n) {
                    AddNode(list + key, n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (key, sItem, tItem) in c.SharedUnequal) {
                if (sItem is INode sn) {
                    if (tItem is INode tn) {
                        var ct = CompareAndUpdateNode(list + key, sn, tn, ref changeSet);
                        if (ct != 0) // 0 = instance is changed, but it passes equality test
                            changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        RemoveNode(list + key, sn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                }
                else {
                    if (tItem is INode tn) {
                        AddNode(list + key, tn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        changeType |= NodeChangeType.PropertyChanged;
                    }
                }
            }
            ReplaceNode(list, source, target, ref changeSet, changeType);
            return changeType;
        }
    }

    [Serializable]
    public class UpdatableIndex<TModel> : UpdatableIndex, IUpdatableIndex<TModel>
        where TModel : class, INode
    {
        protected override INode UntypedModel => Model;
        public TModel Model { get; protected set; }

        [JsonConstructor]
        public UpdatableIndex(TModel model)
        {
            Model = model;
            Reindex();
        }

        protected override void SetModel(INode model) => Model = (TModel) model;
    }
}
