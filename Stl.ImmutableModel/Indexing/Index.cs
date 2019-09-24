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
        INode UntypedModel { get; }
        SymbolList? TryGetPath(INode node);
        INode? TryGetNode(Key key);
        INode? TryGetNodeByPath(SymbolList list);
    }

    public interface IIndex<out TModel> : IIndex
        where TModel : class, INode
    {
        TModel Model { get; }
    }

    [Serializable]
    public abstract class Index : IIndex, INotifyDeserialized
    {
        public static Index<TModel> New<TModel>(TModel model) 
            where TModel : class, INode 
            => new Index<TModel>(model);

        INode IIndex.UntypedModel => UntypedModel;
        protected abstract INode UntypedModel { get; }

        [field: NonSerialized]
        protected ImmutableDictionary<INode, SymbolList> NodeToPath { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<SymbolList, INode> PathToNode { get; set; } = null!;
        [field: NonSerialized]
        protected ImmutableDictionary<Key, INode> DomainKeyToNode { get; set; } = null!;

        protected Index() { }

        public virtual SymbolList? TryGetPath(INode node)
            => NodeToPath.TryGetValue(node, out var path) ? path : null;
        public virtual INode? TryGetNodeByPath(SymbolList list)
            => PathToNode.TryGetValue(list, out var node) ? node : null;
        public virtual INode? TryGetNode(Key key)
            => DomainKeyToNode.TryGetValue(key, out var node) ? node : null;

        protected virtual void Reindex()
        {
            NodeToPath = ImmutableDictionary<INode, SymbolList>.Empty;
            PathToNode = ImmutableDictionary<SymbolList, INode>.Empty;
            DomainKeyToNode = ImmutableDictionary<Key, INode>.Empty;
            var changeSet = ChangeSet.Empty;
            AddNode(SymbolList.Root, UntypedModel, ref changeSet);
        }

        protected virtual void AddNode(SymbolList list, INode node, ref ChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Created);
            NodeToPath = NodeToPath.Add(node, list);
            PathToNode = PathToNode.Add(list, node);
            DomainKeyToNode = DomainKeyToNode.Add(node.Key, node);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                AddNode(list + key, child, ref changeSet);
        }

        protected virtual void RemoveNode(SymbolList list, INode node, ref ChangeSet changeSet)
        {
            changeSet = changeSet.Add(node.Key, NodeChangeType.Removed);
            NodeToPath = NodeToPath.Remove(node);
            PathToNode = PathToNode.Remove(list);
            DomainKeyToNode = DomainKeyToNode.Remove(node.Key);

            foreach (var (key, child) in node.DualGetNodeItems()) 
                RemoveNode(list + key, child, ref changeSet);
        }

        protected virtual void ReplaceNode(SymbolList list, INode source, INode target, 
            ref ChangeSet changeSet, NodeChangeType changeType = NodeChangeType.SubtreeChanged)
        {
            changeSet = changeSet.Add(source.Key, changeType);
            NodeToPath = NodeToPath.Remove(source).Add(target, list);
            PathToNode = PathToNode.SetItem(list, target);
            DomainKeyToNode = DomainKeyToNode.Remove(source.Key).Add(target.Key, target);
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
