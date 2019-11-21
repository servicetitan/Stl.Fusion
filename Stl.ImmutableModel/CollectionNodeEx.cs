using System.Collections;
using System.Collections.Generic;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public static class CollectionNodeEx
    {
        // Regular XxxRange methods

        public static void AddRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<KeyValuePair<Symbol, T>> pairs)
        {
            foreach (var (key, value) in pairs)
                collectionNode.Add(key, value);
        }

        public static void AddOrUpdateRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<KeyValuePair<Symbol, T>> pairs)
        {
            foreach (var (key, value) in pairs)
                collectionNode[key] = value;
        }

        public static void RemoveRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<Symbol> keys)
        {
            foreach (var key in keys)
                collectionNode.Remove(key);
        }

        // Set-style methods for INode-typed items

        public static bool Contains<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.ContainsKey(item.Key.Parts.Tail);

        public static void Add<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.Add(item.Key.Parts.Tail, item);

        public static void AddOrUpdate<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode[item.Key.Parts.Tail] = item;

        public static bool Remove<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.Remove(item.Key.Parts.Tail);

        public static void AddRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<T> nodes)
            where T : INode
        {
            foreach (var node in nodes)
                collectionNode.Add(node);
        }

        public static void AddOrUpdateRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<T> nodes)
            where T : INode
        {
            foreach (var node in nodes)
                collectionNode.AddOrUpdate(node);
        }

        public static void RemoveRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<T> nodes)
            where T : INode
        {
            foreach (var node in nodes)
                collectionNode.Remove(node);
        }
    }
}
