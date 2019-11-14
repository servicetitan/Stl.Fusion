using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public class CollectionNodeTypeDef : NodeTypeDef
    {
        public Type ItemType { get; }
        public bool MayItemBeNode { get; } 
        public bool MayItemBeFreezable { get; } 

        public CollectionNodeTypeDef(Type type) : base(type)
        {
            if (Kind != NodeKind.Collection)
                throw Errors.InternalError("This constructor should be invoked only for ICollectionNode types.");
            
            // Figuring out 
            var tGenericCollectionNode = typeof(ICollectionNode<>);
            var tCollectionInterface = type.GetInterfaces()
                .Single(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == tGenericCollectionNode);
            ItemType = tCollectionInterface.GetGenericArguments().Single();

            MayItemBeNode = ItemType.MayCastSucceed(typeof(NodeBase));
            MayItemBeFreezable = ItemType.MayCastSucceed(typeof(IFreezable));
        }

        public override void FindChildFreezables(INode node, ListBuffer<IFreezable> output)
        {
            if (!MayItemBeFreezable) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var value in collectionNode.Values) {
                if (value is IFreezable f)
                    output.Add(f);
            }
        }

        public override void FindChildNodes(INode node, ListBuffer<INode> output)
        {
            if (!MayItemBeNode) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var value in collectionNode.Values) {
                if (value is INode n)
                    output.Add(n);
            }
        }

        public override IEnumerable<KeyValuePair<Symbol, object?>> GetAllItems(INode node)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode.Items;
        }

        public override bool TryGetItem<T>(INode node, Symbol localKey, out T value)
        {
            var collectionNode = (IDictionary<Symbol, T>) node;
            return collectionNode.TryGetValue(localKey, out value);
        }

        public override bool TryGetItem(INode node, Symbol localKey, out object? value)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode.TryGetValue(localKey, out value);
        }

        public override T GetItem<T>(INode node, Symbol localKey)
        {
            var collectionNode = (IDictionary<Symbol, T>) node;
            return collectionNode[localKey];
        }

        public override object? GetItem(INode node, Symbol localKey)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode[localKey];
        }

        public override void SetItem<T>(INode node, Symbol localKey, T value)
        {
            var collectionNode = (IDictionary<Symbol, T>) node;
            collectionNode[localKey] = value;
        }

        public override void SetItem(INode node, Symbol localKey, object? value)
        {
            var collectionNode = (ICollectionNode) node;
            collectionNode[localKey] = value;
        }

        public override void RemoveItem(INode node, Symbol localKey)
        {
            var collectionNode = (ICollectionNode) node;
            collectionNode.Remove(localKey);
        }
    }
}
