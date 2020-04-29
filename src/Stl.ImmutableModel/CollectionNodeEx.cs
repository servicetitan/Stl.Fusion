using System.Collections.Generic;

namespace Stl.ImmutableModel 
{
    public static class CollectionNodeEx
    {
        // Regular XxxRange methods

        public static void AddRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<KeyValuePair<Key, T>> pairs)
        {
            foreach (var (key, value) in pairs)
                collectionNode.Add(key, value);
        }

        public static void AddOrUpdateRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<KeyValuePair<Key, T>> pairs)
        {
            foreach (var (key, value) in pairs)
                collectionNode[key] = value;
        }

        public static void RemoveRange<T>(this ICollectionNode<T> collectionNode, IEnumerable<Key> keys)
        {
            foreach (var key in keys)
                collectionNode.Remove(key);
        }

        // Set-style methods for INode-typed items

        public static bool Contains<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.ContainsKey(item.Key);

        public static void Add<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.Add(item.Key, item);

        public static void AddOrUpdate<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode[item.Key] = item;

        public static bool Remove<T>(this ICollectionNode<T> collectionNode, T item)
            where T : INode
            => collectionNode.Remove(item.Key);

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
