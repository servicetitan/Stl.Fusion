using System.Collections.Generic;
using System.Linq;

namespace Stl.ImmutableModel 
{
    public static class CollectionNodeEx
    {
        // "With" overloads
        
        public static TNode With<TNode, T>(this TNode node, Symbol localKey, Option<T> item)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(localKey, item);

        public static TNode With<TNode, T>(this TNode node, params (Symbol LocalKey, Option<T> Item)[] changes)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(changes);

        public static TNode With<TNode, T>(this TNode node, IEnumerable<(Symbol LocalKey, Option<T> Item)> changes)
            where TNode : class, ICollectionNode<T>
            => (TNode) node.BaseWith(changes);
        
        // "WithAdded" overloads 
        
        public static TNode WithAdded<TNode, T>(this TNode node, T item)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(item.LocalKey, Option.Some(item));

        public static TNode WithAdded<TNode, T>(this TNode node, params T[] items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (Key: i.LocalKey, Option.Some(i))));

        public static TNode WithAdded<TNode, T>(this TNode node, IEnumerable<T> items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (Key: i.LocalKey, Option.Some(i))));
        
        // WithRemoved overloads
        
        public static TNode WithRemoved<TNode, T>(this TNode node, T item)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(item.LocalKey, default);

        public static TNode WithRemoved<TNode, T>(this TNode node, params T[] items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (Key: i.LocalKey, Option<T>.None)));

        public static TNode WithRemoved<TNode, T>(this TNode node, IEnumerable<T> items)
            where TNode : class, ICollectionNode<T>
            where T : class, INode
            => (TNode) node.BaseWith(items.Select(i => (Key: i.LocalKey, Option<T>.None)));

        public static TNode WithRemoved<TNode>(this TNode node, Symbol localKey)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(localKey, default);

        public static TNode WithRemoved<TNode>(this TNode node, params Symbol[] keys)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(keys.Select(k => (k, Option<object?>.None)));

        public static TNode WithRemoved<TNode>(this TNode node, IEnumerable<Symbol> keys)
            where TNode : class, ICollectionNode
            => (TNode) node.BaseWith(keys.Select(k => (k, Option<object?>.None)));
    }
}
