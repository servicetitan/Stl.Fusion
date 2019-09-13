using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl;
using Stl.Internal;
using Stl.Serialization;
using Errors = Stl.ImmutableModel.Internal.Errors;

namespace Stl.ImmutableModel
{
    public interface IIndex
    {
        INode UntypedModel { get; }
        SymbolPath? TryGetPath(INode node);
        INode? TryGetNode(SymbolPath path);
        INode? TryGetNode(DomainKey domainKey);
    }

    public interface IUpdateableIndex : IIndex
    {
        (IUpdateableIndex Index, ChangeSet ChangeSet) BaseUpdate(INode source, INode target);
    }

    public interface IIndex<out TModel> : IIndex
        where TModel : class, INode
    {
        TModel Model { get; }
    }

    public interface IUpdateableIndex<out TModel> : IIndex<TModel>, IUpdateableIndex
        where TModel : class, INode
    { }

    [Serializable]
    public class Index : IUpdateableIndex, INotifyDeserialized
    {
        public static Index<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new Index<TModel>(model);

        public INode UntypedModel { get; protected set; }

        [field: NonSerialized]
        protected ImmutableDictionary<INode, SymbolPath> NodeToPath { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<SymbolPath, INode> PathToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<DomainKey, INode> DomainKeyToNode { get; set; } = null!;

        [JsonConstructor]
        public Index(INode untypedModel)
        {
            UntypedModel = untypedModel;
            Reindex();
        }

        public virtual SymbolPath? TryGetPath(INode node)
            => NodeToPath.TryGetValue(node, out var path) ? path : null;
        public virtual INode? TryGetNode(SymbolPath path)
            => PathToNode.TryGetValue(path, out var node) ? node : null;
        public virtual INode? TryGetNode(DomainKey domainKey)
            => DomainKeyToNode.TryGetValue(domainKey, out var node) ? node : null;

        public virtual (IUpdateableIndex Index, ChangeSet ChangeSet) BaseUpdate(
            INode source, INode target)
        {
            if (source == target)
                return (this, ChangeSet.Empty);

            if (source.Key != target.Key)
                throw Errors.InvalidUpdateKeyMismatch();
            
            var clone = (Index) MemberwiseClone();
            var changeSet = new ChangeSet();
            clone.UpdateNode(source, target, ref changeSet);
            return (clone, changeSet);
        }

        protected virtual void UpdateNode(INode source, INode target, ref ChangeSet changeSet)
        {
            SymbolPath? path = this.GetPath(source);
            CompareNode(path, source, target, ref changeSet);

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

        private void CompareNode(SymbolPath path, INode source, INode target, ref ChangeSet changeSet)
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
                        CompareNode(path + key, sn, tn, ref changeSet);
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

        protected virtual void AddNode(SymbolPath path, INode node, ref ChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.DomainKey, ChangeKind.Added);
            NodeToPath = NodeToPath.Add(node, path);
            PathToNode = PathToNode.Add(path, node);
            DomainKeyToNode = DomainKeyToNode.Add(node.DomainKey, node);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                AddNode(path + key, child, ref changeSet);
        }

        protected virtual void RemoveNode(SymbolPath path, INode node, ref ChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.DomainKey, ChangeKind.Removed);
            NodeToPath = NodeToPath.Remove(node);
            PathToNode = PathToNode.Remove(path);
            DomainKeyToNode = DomainKeyToNode.Remove(node.DomainKey);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                RemoveNode(path + key, child, ref changeSet);
        }

        protected virtual void ReplaceNode(SymbolPath path, INode source, INode target, 
            ref ChangeSet changeSet, ChangeKind changeKind = ChangeKind.SubtreeChanged)
        {
            changeSet = changeSet.Add(source.DomainKey, changeKind);
            NodeToPath = NodeToPath.Remove(source).Add(target, path);
            PathToNode = PathToNode.SetItem(path, target);
            DomainKeyToNode = DomainKeyToNode.Remove(source.DomainKey).Add(target.DomainKey, target);
        }

        protected void Reindex()
        {
            NodeToPath = ImmutableDictionary<INode, SymbolPath>.Empty;
            PathToNode = ImmutableDictionary<SymbolPath, INode>.Empty;
            DomainKeyToNode = ImmutableDictionary<DomainKey, INode>.Empty;
            var changeSet = ChangeSet.Empty;
            AddNode(new SymbolPath(UntypedModel.Key), UntypedModel, ref changeSet);
        }

        // Serialization
        
        // Complex, b/c JSON.NET doesn't allow [OnDeserialized] methods to be virtual
        [OnDeserialized] protected void OnDeserializedHandler(StreamingContext context) => OnDeserialized(context);
        void INotifyDeserialized.OnDeserialized(StreamingContext context) => OnDeserialized(context);
        protected virtual void OnDeserialized(StreamingContext context)
        {
            if (UntypedModel is INotifyDeserialized d)
                d.OnDeserialized(context);
            if (NodeToPath == null) 
                // Regular serialization, not JSON.NET
                Reindex();
        }
    }

    [Serializable]
    public class Index<TModel> : Index, IUpdateableIndex<TModel>
        where TModel : class, INode
    {
        [JsonIgnore] public TModel Model => (TModel) UntypedModel;

        [JsonConstructor]
        public Index(TModel untypedModel) : base(untypedModel) { }
    }
}
