using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Comparison;
using Stl.ImmutableModel.Internal;
using Stl.ImmutableModel.Reflection;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Indexing
{
    public interface IModelIndex
    {
        INode Model { get; }
        IEnumerable<(INode Node, NodeLink NodeLink)> Entries { get; }
        INode? TryGetNode(Key key);
        INode? TryGetNode(NodeLink nodeLink);
        NodeLink? TryGetNodeLink(INode node);
        (IModelIndex Index, ModelChangeSet ChangeSet) BaseWith(INode source, INode target);
    }

    public interface IModelIndex<out TModel> : IModelIndex
        where TModel : class, INode
    {
        new TModel Model { get; }
    }

    [Serializable]
    public abstract class ModelIndex : IModelIndex
    {
        public static ModelIndex<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new ModelIndex<TModel>(model);

        INode IModelIndex.Model => Model;
        protected INode Model { get; private set; } = null!;

        [field: NonSerialized]
        protected ImmutableDictionary<Key, INode> KeyToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<NodeLink, INode> NodeLinkToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<INode, NodeLink> NodeToNodeLink { get; set; } = null!;

        [JsonIgnore]
        public IEnumerable<(INode Node, NodeLink NodeLink)> Entries 
            => NodeToNodeLink.Select(p => (p.Key, p.Value));

        public INode? TryGetNode(Key key)
            => KeyToNode.TryGetValue(key, out var node) ? node : null;
        public INode? TryGetNode(NodeLink nodeLink)
            => NodeLinkToNode.TryGetValue(nodeLink, out var node) ? node : null;
        public NodeLink? TryGetNodeLink(INode node)
            => NodeToNodeLink.TryGetValue(node, out var path) ? (NodeLink?) path : null;

        public virtual (IModelIndex Index, ModelChangeSet ChangeSet) BaseWith(
            INode source, INode target)
        {
            if (source == target)
                return (this, ModelChangeSet.Empty);

            if (source.Key != target.Key)
                throw Errors.InvalidUpdateKeyMismatch();
            
            var clone = (ModelIndex) MemberwiseClone();
            var changeSet = ModelChangeSet.Empty;
            clone.UpdateNode(source, target, ref changeSet);
            return (clone, changeSet);
        }

        protected virtual void SetModel(INode model)
        {
            model.Freeze();
            Model = model;
            KeyToNode = ImmutableDictionary<Key, INode>.Empty;
            NodeLinkToNode = ImmutableDictionary<NodeLink, INode>.Empty;
            NodeToNodeLink = ImmutableDictionary<INode, NodeLink>.Empty;
            var changeSet = ModelChangeSet.Empty;
            AddNode(NodeLink.None, Model, ref changeSet);
            model.DiscardChangeHistory();
        }

        protected virtual void UpdateNode(INode source, INode target, ref ModelChangeSet changeSet)
        {
            var nodeLink = this.GetNodeLink(source);
            target.Freeze();
            CompareAndUpdateNode(nodeLink, source, target, ref changeSet);
            target.DiscardChangeHistory();

            while (nodeLink.ParentKey != null) {
                var sourceParent = this.GetNode(nodeLink.ParentKey);
                var targetParent = sourceParent.ToUnfrozen();
                var nodeTypeDef = targetParent.GetDefinition();
                nodeTypeDef.SetItem(targetParent, nodeLink.ItemKey, (object?) target);
                targetParent.Freeze();
                nodeLink = this.GetNodeLink(sourceParent);
                ReplaceNode(nodeLink, sourceParent, targetParent, ref changeSet);
                targetParent.DiscardChangeHistory();
                source = sourceParent;
                target = targetParent;
            }
            SetModel(target);
        }

        protected virtual void AddNode(NodeLink nodeLink, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Created);
            KeyToNode = KeyToNode.Add(node.Key, node);
            NodeLinkToNode = NodeLinkToNode.Add(nodeLink, node);
            NodeToNodeLink = NodeToNodeLink.Add(node, nodeLink);

            var nodeTypeDef = node.GetDefinition();
            var buffer = ListBuffer<KeyValuePair<ItemKey, INode>>.Lease();
            try {
                nodeTypeDef.GetNodeItems(node, ref buffer);
                var parentKey = node.Key;

                foreach (var (itemKey, child) in buffer) 
                    AddNode((parentKey, itemKey), child, ref changeSet);
            }
            finally {
                buffer.Release();
            }
        }

        protected virtual void RemoveNode(NodeLink nodeLink, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Removed);
            KeyToNode = KeyToNode.Remove(node.Key);
            NodeLinkToNode = NodeLinkToNode.Remove(nodeLink);
            NodeToNodeLink = NodeToNodeLink.Remove(node);

            var nodeTypeDef = node.GetDefinition();
            var buffer = ListBuffer<KeyValuePair<ItemKey, INode>>.Lease();
            try {
                nodeTypeDef.GetNodeItems(node, ref buffer);
                var parentKey = node.Key;

                foreach (var (itemKey, child) in buffer) 
                    RemoveNode((parentKey, itemKey), child, ref changeSet);
            }
            finally {
                buffer.Release();
            }
        }

        protected virtual void ReplaceNode(NodeLink nodeLink, INode source, INode target, 
            ref ModelChangeSet changeSet, NodeChangeType changeType = NodeChangeType.SubtreeChanged)
        {
            changeSet = changeSet.Add(source.Key, changeType);
            KeyToNode = KeyToNode.Remove(source.Key).Add(target.Key, target);
            NodeLinkToNode = NodeLinkToNode.SetItem(nodeLink, target);
            NodeToNodeLink = NodeToNodeLink.Remove(source).Add(target, nodeLink);
        }

        private NodeChangeType CompareAndUpdateNode(NodeLink sourceLink, INode source, INode target, ref ModelChangeSet changeSet)
        {
            if (source == target)
                return 0;

            var changeType = (NodeChangeType) 0;
            if (source.GetType() != target.GetType())
                changeType |= NodeChangeType.TypeChanged;

            var sPairs = source.GetDefinition().GetAllItems(source).ToDictionary();
            var tPairs = target.GetDefinition().GetAllItems(target).ToDictionary();
            var c = DictionaryComparison.New(sPairs, tPairs);
            if (c.AreEqual) {
                if (changeType != 0)
                    ReplaceNode(sourceLink, source, target, ref changeSet, changeType);
                return changeType;
            }

            var parentKey = source.Key;
            foreach (var (itemKey, item) in c.LeftOnly) {
                if (item is INode n) {
                    RemoveNode((parentKey, itemKey), n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (itemKey, item) in c.RightOnly) {
                if (item is INode n) {
                    AddNode((parentKey, itemKey), n, ref changeSet);
                    changeType |= NodeChangeType.SubtreeChanged;
                }
            }
            foreach (var (itemKey, sItem, tItem) in c.SharedUnequal) {
                if (sItem is INode sn) {
                    if (tItem is INode tn) {
                        var ct = CompareAndUpdateNode((parentKey, itemKey), sn, tn, ref changeSet);
                        if (ct != 0) // 0 = instance is changed, but it passes equality test
                            changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        RemoveNode((parentKey, itemKey), sn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                }
                else {
                    if (tItem is INode tn) {
                        AddNode((parentKey, itemKey), tn, ref changeSet);
                        changeType |= NodeChangeType.SubtreeChanged;
                    }
                    else {
                        changeType |= NodeChangeType.PropertyChanged;
                    }
                }
            }
            ReplaceNode(sourceLink, source, target, ref changeSet, changeType);
            return changeType;
        }

        // Serialization
        
        // Complex, b/c JSON.NET doesn't allow [OnDeserialized] methods to be virtual
        [OnDeserialized] protected void OnDeserializedHandler(StreamingContext context) => OnDeserialized(context);
        protected virtual void OnDeserialized(StreamingContext context)
        {
            if (KeyToNode == null) 
                // Regular serialization, not JSON.NET
                SetModel(Model);
        }
    }

    [Serializable]
    public class ModelIndex<TModel> : ModelIndex, IModelIndex<TModel>
        where TModel : class, INode
    {
        public new TModel Model { get; private set; } = null!;

        // This constructor is to be used by descendants,
        // since the public one also triggers indexing.
        protected ModelIndex() { }

        [JsonConstructor]
        public ModelIndex(TModel model) => SetModel(model);

        protected override void SetModel(INode model)
        {
            Model = (TModel) model;
            base.SetModel(model);
        }
    }
}
