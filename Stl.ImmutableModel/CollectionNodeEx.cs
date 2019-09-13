using System.Collections.Generic;
using System.Linq;

namespace Stl.ImmutableModel 
{
    public static class CollectionNodeEx
    {
        // "With" overloads
        
        public static TNode With<TNode, T>(this TNode node, Key key, Option<T> item)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(key, item);

        public static TNode With<TNode, T>(this TNode node, params (Key Key, Option<T> Item)[] changes)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(changes);

        public static TNode With<TNode, T>(this TNode node, IEnumerable<(Key Key, Option<T> Item)> changes)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(changes);
        
        // "WithAdded" overloads 
        
        public static TNode WithAdded<TNode, T>(this TNode node, T item)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(item.Key, Option.Some(item));

        public static TNode WithAdded<TNode, T>(this TNode node, params T[] items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (i.Key, Option.Some(i))));

        public static TNode WithAdded<TNode, T>(this TNode node, IEnumerable<T> items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (i.Key, Option.Some(i))));
        
        // WithRemoved overloads
        
        public static TNode WithRemoved<TNode, T>(this TNode node, T item)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(item.Key, default);

        public static TNode WithRemoved<TNode, T>(this TNode node, params T[] items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (i.Key, Option<T>.None)));

        public static TNode WithRemoved<TNode, T>(this TNode node, IEnumerable<T> items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (i.Key, Option<T>.None)));

        public static TNode WithRemoved<TNode>(this TNode node, Key key)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(key, default);

        public static TNode WithRemoved<TNode>(this TNode node, params Key[] keys)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(keys.Select(k => (k, Option<object?>.None)));

        public static TNode WithRemoved<TNode>(this TNode node, IEnumerable<Key> keys)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(keys.Select(k => (k, Option<object?>.None)));
    }
}
