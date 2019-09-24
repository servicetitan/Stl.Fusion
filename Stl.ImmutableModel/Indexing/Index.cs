using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.ImmutableModel.Updating;
using Stl.Serialization;

namespace Stl.ImmutableModel.Indexing
{
    public interface IIndex
    {
        INode Model { get; }
        INode? TryGetNode(Key key);
        INode? TryGetNodeByPath(SymbolList list);
        SymbolList? TryGetPath(INode node);
    }

    public interface IIndex<out TModel> : IIndex
        where TModel : class, INode
    {
        new TModel Model { get; }
    }

    [Serializable]
    public abstract class Index : IIndex, INotifyDeserialized
    {
        public static Index<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new Index<TModel>(model);

        INode IIndex.Model => UntypedModel;
        protected abstract INode UntypedModel { get; }

        [field: NonSerialized]
        protected ImmutableDictionary<Key, INode> KeyToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<SymbolList, INode> PathToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<INode, SymbolList> NodeToPath { get; set; } = null!;

        protected Index() { }

        public virtual INode? TryGetNode(Key key)
            => KeyToNode.TryGetValue(key, out var node) ? node : null;
        public virtual INode? TryGetNodeByPath(SymbolList list)
            => PathToNode.TryGetValue(list, out var node) ? node : null;
        public virtual SymbolList? TryGetPath(INode node)
            => NodeToPath.TryGetValue(node, out var path) ? path : null;

        protected virtual void Reindex()
        {
            KeyToNode = ImmutableDictionary<Key, INode>.Empty;
            PathToNode = ImmutableDictionary<SymbolList, INode>.Empty;
            NodeToPath = ImmutableDictionary<INode, SymbolList>.Empty;
            var changeSet = ModelChangeSet.Empty;
            AddNode(SymbolList.Root, UntypedModel, ref changeSet);
        }

        protected virtual void AddNode(SymbolList list, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Created);
            KeyToNode = KeyToNode.Add(node.Key, node);
            PathToNode = PathToNode.Add(list, node);
            NodeToPath = NodeToPath.Add(node, list);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                AddNode(list + key, child, ref changeSet);
        }

        protected virtual void RemoveNode(SymbolList list, INode node, ref ModelChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Removed);
            KeyToNode = KeyToNode.Remove(node.Key);
            PathToNode = PathToNode.Remove(list);
            NodeToPath = NodeToPath.Remove(node);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                RemoveNode(list + key, child, ref changeSet);
        }

        protected virtual void ReplaceNode(SymbolList list, INode source, INode target, 
            ref ModelChangeSet changeSet, NodeChangeType changeType = NodeChangeType.SubtreeChanged)
        {
            changeSet = changeSet.Add(source.Key, changeType);
            KeyToNode = KeyToNode.Remove(source.Key).Add(target.Key, target);
            PathToNode = PathToNode.SetItem(list, target);
            NodeToPath = NodeToPath.Remove(source).Add(target, list);
        }

        // Serialization
        
        // Complex, b/c JSON.NET doesn't allow [OnDeserialized] methods to be virtual
        [OnDeserialized] protected void OnDeserializedHandler(StreamingContext context) => OnDeserialized(context);
        void INotifyDeserialized.OnDeserialized(StreamingContext context) => OnDeserialized(context);
        protected virtual void OnDeserialized(StreamingContext context)
        {
            if (UntypedModel is INotifyDeserialized d)
                d.OnDeserialized(context);
            if (KeyToNode == null) 
                // Regular serialization, not JSON.NET
                Reindex();
        }
    }

    [Serializable]
    public class Index<TModel> : Index, IIndex<TModel>
        where TModel : class, INode
    {
        protected override INode UntypedModel => Model;
        public TModel Model { get; }

        [JsonConstructor]
        public Index(TModel model)
        {
            Model = model;
            Reindex();
        }
    }
}
