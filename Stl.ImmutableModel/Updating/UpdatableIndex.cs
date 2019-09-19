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
        (IUpdatableIndex Index, ChangeSet ChangeSet) BaseUpdate(INode source, INode target);
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

        protected abstract void SetUntypedModel(INode model);

        public virtual (IUpdatableIndex Index, ChangeSet ChangeSet) BaseUpdate(
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
            SetUntypedModel(target);
        }

        private NodeChangeType CompareAndUpdateNode(SymbolPath path, INode source, INode target, ref ChangeSet changeSet)
        {
            if (source == target)
                return 0;

            var changeType = (NodeChangeType) 0;
            var sPairs = source.DualGetItems().ToDictionary();
            var tPairs = target.DualGetItems().ToDictionary();
            var c = DictionaryComparison.New(sPairs, tPairs);
            if (c.AreEqual)
                return changeType;

            foreach (var (key, item) in c.LeftOnly) {
                if (item is INode n) {
                    RemoveNode(path + key, n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (key, item) in c.RightOnly) {
                if (item is INode n) {
                    AddNode(path + key, n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (key, sItem, tItem) in c.SharedUnequal) {
                if (sItem is INode sn) {
                    if (tItem is INode tn) {
                        var ct = CompareAndUpdateNode(path + key, sn, tn, ref changeSet);
                        if (ct == 0)
                            throw Stl.Internal.Errors.InternalError(
                                "CompareAndUpdate returned 0 for SharedUnequal item.");
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        RemoveNode(path + key, sn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                }
                else {
                    if (tItem is INode tn) {
                        AddNode(path + key, tn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        changeType |= NodeChangeType.PropertyChanged;
                    }
                }
            }
            ReplaceNode(path, source, target, ref changeSet, changeType);
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

        protected override void SetUntypedModel(INode model) => Model = (TModel) model;
    }

}
