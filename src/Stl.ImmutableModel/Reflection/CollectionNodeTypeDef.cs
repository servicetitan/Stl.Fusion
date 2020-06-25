using System;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.Frozen;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel.Reflection
{
    public class CollectionNodeTypeDef : NodeTypeDef
    {
        public Type ItemType { get; }

        public bool IsItemFrozen { get; }
        public bool IsItemNode { get; }
        public bool IsItemSimpleNode { get; }
        public bool IsItemCollectionNode { get; }

        public bool MayItemBeFrozen { get; } 
        public bool MayItemBeNode { get; } 
        public bool MayItemBeSimpleNode { get; } 
        public bool MayItemBeCollectionNode { get; } 

        public CollectionNodeTypeDef(Type type) : base(type)
        {
            if (!IsCollectionNode)
                throw Errors.InternalError("This constructor should be invoked only for collection node types.");
            
            // Figuring out item type & other stuff 
            var tGenericCollectionNode = typeof(ICollectionNode<>);
            var tCollectionInterface = type.GetInterfaces()
                .Single(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == tGenericCollectionNode);
            ItemType = tCollectionInterface.GetGenericArguments().Single();

            IsItemFrozen = typeof(IFrozen).IsAssignableFrom(ItemType);
            IsItemNode = typeof(Node).IsAssignableFrom(ItemType);
            IsItemSimpleNode = typeof(Node).IsAssignableFrom(ItemType);
            IsItemCollectionNode = typeof(CollectionNodeBase).IsAssignableFrom(ItemType);
            
            MayItemBeFrozen = ItemType.MayCastSucceed(typeof(IFrozen));
            MayItemBeNode = ItemType.MayCastSucceed(typeof(Node));
            MayItemBeSimpleNode = ItemType.MayCastSucceed(typeof(Node));
            MayItemBeCollectionNode = ItemType.MayCastSucceed(typeof(CollectionNodeBase));
        }

        public override IEnumerable<KeyValuePair<ItemKey, object?>> GetAllItems(INode node)
        {
            var collectionNode = (ICollectionNode) node;
            var collectionItems = collectionNode.Items.Select(p => KeyValuePair.Create((ItemKey) p.Key, p.Value));
            return base.GetAllItems(node).Concat(collectionItems);
        }

        public override void GetFrozenItems(INode node, ref MemoryBuffer<KeyValuePair<ItemKey, IFrozen>> output)
        {
            base.GetFrozenItems(node, ref output);
            if (!IsItemFrozen) return;

            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is IFrozen f)
                    output.Add(KeyValuePair.Create((ItemKey) key, f));
            }
        }

        public override void GetNodeItems(INode node, ref MemoryBuffer<KeyValuePair<ItemKey, INode>> output)
        {
            base.GetNodeItems(node, ref output);
            if (!IsItemNode) return;

            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is INode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override void GetCollectionNodeItems(INode node, ref MemoryBuffer<KeyValuePair<ItemKey, ICollectionNode>> output)
        {
            base.GetCollectionNodeItems(node, ref output);
            if (!IsItemCollectionNode) return;

            var collectionNode = (ICollectionNode) node;
            foreach (var (key, value) in collectionNode.Items) {
                if (value is ICollectionNode n)
                    output.Add(KeyValuePair.Create((ItemKey) key, n));
            }
        }

        public override bool TryGetItem<T>(INode node, ItemKey itemKey, out T value)
        {
            if (itemKey.IsSymbol)
                return base.TryGetItem(node, itemKey, out value);
            var collectionNode = (IDictionary<Key, T>) node;
            return collectionNode.TryGetValue(itemKey.AsKey(), out value);
        }

        public override bool TryGetItem(INode node, ItemKey itemKey, out object? value)
        {
            if (itemKey.IsSymbol)
                return base.TryGetItem(node, itemKey, out value);
            var collectionNode = (ICollectionNode) node;
            return collectionNode.TryGetValue(itemKey.AsKey(), out value);
        }

        public override T GetItem<T>(INode node, ItemKey itemKey)
        {
            if (itemKey.IsSymbol)
                return base.GetItem<T>(node, itemKey);
            var collectionNode = (IDictionary<Key, T>) node;
            return collectionNode[itemKey.AsKey()];
        }

        public override object? GetItem(INode node, ItemKey itemKey)
        {
            if (itemKey.IsSymbol)
                return base.GetItem(node, itemKey);
            var collectionNode = (ICollectionNode) node;
            return collectionNode[itemKey.AsKey()];
        }

        public override void SetItem<T>(INode node, ItemKey itemKey, T value)
        {
            if (itemKey.IsSymbol) {
                base.SetItem(node, itemKey, value);
                return;
            }
            var collectionNode = (IDictionary<Key, T>) node;
            collectionNode[itemKey.AsKey()] = value;
        }

        public override void SetItem(INode node, ItemKey itemKey, object? value)
        {
            if (itemKey.IsSymbol) {
                base.SetItem(node, itemKey, value);
                return;
            }
            var collectionNode = (ICollectionNode) node;
            collectionNode[itemKey.AsKey()] = value;
        }

        public override void RemoveItem(INode node, ItemKey itemKey)
        {
            if (itemKey.IsSymbol) {
                base.RemoveItem(node, itemKey);
                return;
            }
            var collectionNode = (ICollectionNode) node;
            collectionNode.Remove(itemKey.AsKey());
        }
    }
}
