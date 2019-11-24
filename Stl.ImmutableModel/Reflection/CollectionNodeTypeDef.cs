using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel.Reflection
{
    public class CollectionNodeTypeDef : NodeTypeDef
    {
        public Type ItemType { get; }

        public bool IsItemFreezable { get; }
        public bool IsItemNode { get; }
        public bool IsItemSimpleNode { get; }
        public bool IsItemCollectionNode { get; }

        public bool MayItemBeFreezable { get; } 
        public bool MayItemBeNode { get; } 
        public bool MayItemBeSimpleNode { get; } 
        public bool MayItemBeCollectionNode { get; } 

        public CollectionNodeTypeDef(Type type) : base(type)
        {
            if (Kind != NodeKind.Collection)
                throw Errors.InternalError("This constructor should be invoked only for ICollectionNode types.");
            
            // Figuring out 
            var tGenericCollectionNode = typeof(ICollectionNode<>);
            var tCollectionInterface = type.GetInterfaces()
                .Single(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == tGenericCollectionNode);
            ItemType = tCollectionInterface.GetGenericArguments().Single();

            IsItemFreezable = typeof(IFreezable).IsAssignableFrom(ItemType);
            IsItemNode = typeof(NodeBase).IsAssignableFrom(ItemType);
            IsItemSimpleNode = typeof(SimpleNodeBase).IsAssignableFrom(ItemType);
            IsItemCollectionNode = typeof(CollectionNodeBase).IsAssignableFrom(ItemType);
            
            MayItemBeFreezable = ItemType.MayCastSucceed(typeof(IFreezable));
            MayItemBeNode = ItemType.MayCastSucceed(typeof(NodeBase));
            MayItemBeSimpleNode = ItemType.MayCastSucceed(typeof(SimpleNodeBase));
            MayItemBeCollectionNode = ItemType.MayCastSucceed(typeof(CollectionNodeBase));
        }

        public override IEnumerable<KeyValuePair<ItemKey, object?>> GetAllItems(INode node)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode.Items
                .Select(p => KeyValuePair.Create((ItemKey) p.Key, p.Value));
        }

        public override void GetFreezableItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, IFreezable>> output)
        {
            if (!IsItemFreezable) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is IFreezable f)
                    output.Add(KeyValuePair.Create((ItemKey) key, f));
            }
        }

        public override void GetNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, INode>> output)
        {
            if (!IsItemNode) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override void GetCollectionNodeItems(INode node, ref ListBuffer<KeyValuePair<ItemKey, ICollectionNode>> output)
        {
            if (!IsItemCollectionNode) return;
            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override bool TryGetItem<T>(INode node, ItemKey itemKey, out T value)
        {
            var collectionNode = (IDictionary<Key, T>) node;
            return collectionNode.TryGetValue(itemKey.AsKey(), out value);
        }

        public override bool TryGetItem(INode node, ItemKey itemKey, out object? value)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode.TryGetValue(itemKey.AsKey(), out value);
        }

        public override T GetItem<T>(INode node, ItemKey itemKey)
        {
            var collectionNode = (IDictionary<Key, T>) node;
            return collectionNode[itemKey.AsKey()];
        }

        public override object? GetItem(INode node, ItemKey itemKey)
        {
            var collectionNode = (ICollectionNode) node;
            return collectionNode[itemKey.AsKey()];
        }

        public override void SetItem<T>(INode node, ItemKey itemKey, T value)
        {
            var collectionNode = (IDictionary<Key, T>) node;
            collectionNode[itemKey.AsKey()] = value;
        }

        public override void SetItem(INode node, ItemKey itemKey, object? value)
        {
            var collectionNode = (ICollectionNode) node;
            collectionNode[itemKey.AsKey()] = value;
        }

        public override void RemoveItem(INode node, ItemKey itemKey)
        {
            var collectionNode = (ICollectionNode) node;
            collectionNode.Remove(itemKey.AsKey());
        }
    }
}
