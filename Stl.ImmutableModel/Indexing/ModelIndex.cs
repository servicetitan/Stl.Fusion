using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Comparison;
using Stl.ImmutableModel.Internal;
using Stl.ImmutableModel.Updating;
using Stl.Serialization;

namespace Stl.ImmutableModel.Indexing
{
    public interface IModelIndex
    {
        INode Model { get; }
        IEnumerable<(INode Node, SymbolList Path)> Entries { get; }
        INode? TryGetNode(Key key);
        INode? TryGetNodeByPath(SymbolList list);
        SymbolList? TryGetPath(INode node);
        (IModelIndex Index, ModelChangeSet ChangeSet) BaseWith(INode source, INode target);
    }

    public interface IModelIndex<out TModel> : IModelIndex
        where TModel : class, INode
    {
        new TModel Model { get; }
    }

    [Serializable]
    public abstract class ModelIndex : IModelIndex, INotifyDeserialized
    {
        public static ModelIndex<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new ModelIndex<TModel>(model);

        INode IModelIndex.Model => Model;
        protected INode Model { get; private set; } = null!;

        [field: NonSerialized]
        protected ImmutableDictionary<Key, INode> KeyToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<SymbolList, INode> PathToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<INode, SymbolList> NodeToPath { get; set; } = null!;

        [JsonIgnore]
        public IEnumerable<(INode Node, SymbolList Path)> Entries 
            => NodeToPath.Select(p => (p.Key, p.Value));

        public INode? TryGetNode(Key key)
            => KeyToNode.TryGetValue(key, out var node) ? node : null;
        public INode? TryGetNodeByPath(SymbolList list)
            => PathToNode.TryGetValue(list, out var node) ? node : null;
        public SymbolList? TryGetPath(INode node)
            => NodeToPath.TryGetValue(node, out var path) ? path : null;

        protected virtual void SetModel(INode model)
        {
            model.Freeze();
            Model = model;
            KeyToNode = ImmutableDictionary<Key, INode>.Empty;
            PathToNode = ImmutableDictionary<SymbolList, INode>.Empty;
            NodeToPath = ImmutableDictionary<INode, SymbolList>.Empty;
            var changeSet = ModelChangeSet.Empty;
            AddNode(SymbolList.Empty, Model, ref changeSet);
            model.DiscardChangeHistory();
        }

        protected virtual void AddNode(SymbolList path, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Created);
            KeyToNode = KeyToNode.Add(node.Key, node);
            PathToNode = PathToNode.Add(path, node);
            NodeToPath = NodeToPath.Add(node, path);

            var nodeTypeDef = node.GetDefinition();
            var buffer = ListBuffer<KeyValuePair<Symbol, INode>>.Lease();
            try {
                nodeTypeDef.GetNodeItems(node, ref buffer);

                foreach (var (key, child) in buffer) 
                    AddNode(path + key, child, ref changeSet);
            }
            finally {
                buffer.Release();
            }
        }

        protected virtual void RemoveNode(SymbolList path, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Removed);
            KeyToNode = KeyToNode.Remove(node.Key);
            PathToNode = PathToNode.Remove(path);
            NodeToPath = NodeToPath.Remove(node);

            var nodeTypeDef = node.GetDefinition();
            var buffer = ListBuffer<KeyValuePair<Symbol, INode>>.Lease();
            try {
                nodeTypeDef.GetNodeItems(node, ref buffer);

                foreach (var (key, child) in buffer) 
                    RemoveNode(path + key, child, ref changeSet);
            }
            finally {
                buffer.Release();
            }
        }

        protected virtual void ReplaceNode(SymbolList path, INode source, INode target, 
            ref ModelChangeSet changeSet, NodeChangeType changeType = NodeChangeType.SubtreeChanged)
        {
            changeSet = changeSet.Add(source.Key, changeType);
            KeyToNode = KeyToNode.Remove(source.Key).Add(target.Key, target);
            PathToNode = PathToNode.SetItem(path, target);
            NodeToPath = NodeToPath.Remove(source).Add(target, path);
        }

        public virtual (IModelIndex Index, ModelChangeSet ChangeSet) BaseWith(
            INode source, INode target)
        {
            if (source == target)
                return (this, ModelChangeSet.Empty);

            if (source.LocalKey != target.LocalKey)
                throw Errors.InvalidUpdateKeyMismatch();
            
            var clone = (ModelIndex) MemberwiseClone();
            var changeSet = new ModelChangeSet();
            clone.UpdateNode(source, target, ref changeSet);
            return (clone, changeSet);
        }

        protected virtual void UpdateNode(INode source, INode target, ref ModelChangeSet changeSet)
        {
            SymbolList? path = this.GetPath(source);
            target.Freeze();
            CompareAndUpdateNode(path, source, target, ref changeSet);
            target.DiscardChangeHistory();

            var tail = path.Tail;
            path = path.Prefix;
            while (path != null) {
                var sourceParent = this.GetNodeByPath(path);
                var targetParent = sourceParent.ToUnfrozen();
                targetParent.GetDefinition().SetItem(targetParent, tail, (object?) target);
                targetParent.Freeze();
                ReplaceNode(path, sourceParent, targetParent, ref changeSet);
                targetParent.DiscardChangeHistory();
                source = sourceParent;
                target = targetParent;
                tail = path.Tail;
                path = path.Prefix;
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

            var sPairs = source.GetDefinition().GetAllItems(source).ToDictionary();
            var tPairs = target.GetDefinition().GetAllItems(target).ToDictionary();
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

        // Serialization
        
        // Complex, b/c JSON.NET doesn't allow [OnDeserialized] methods to be virtual
        [OnDeserialized] protected void OnDeserializedHandler(StreamingContext context) => OnDeserialized(context);
        void INotifyDeserialized.OnDeserialized(StreamingContext context) => OnDeserialized(context);
        protected virtual void OnDeserialized(StreamingContext context)
        {
            if (Model is INotifyDeserialized d)
                d.OnDeserialized(context);
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
