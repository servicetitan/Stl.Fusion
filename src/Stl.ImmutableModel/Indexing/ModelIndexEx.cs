using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Indexing
{
    public static class ModelIndexEx
    {
        // GetXxx

        public static INode? GetParent(this IModelIndex index, Key key)
            => index.GetParent(index.GetNode(key));
        public static INode? GetParent(this IModelIndex index, INode node)
        {
            var itemRef = index.GetNodeLink(node);
            return itemRef.ParentKey == null ? null : index.GetNode(itemRef.ParentKey);
        }

        public static IEnumerable<INode> GetParents(this IModelIndex index, Key key, bool includeSelf = false)
            => index.GetParents(index.GetNode(key), includeSelf);
        public static IEnumerable<INode> GetParents(this IModelIndex index, INode node, bool includeSelf = false)
        {
            if (includeSelf)
                yield return node;
            INode? n = node;
            while ((n = index.GetParent(n)) != null)
                yield return n;
        }

        // GetNodeLink

        public static NodeLink GetNodeLink(this IModelIndex index, INode node) 
            => index.TryGetNodeLink(node) ?? throw new KeyNotFoundException();
        
        // GetNode (non-generic)

        public static INode GetNode(this IModelIndex index, Key key)
            => index.TryGetNode(key) ?? throw new KeyNotFoundException();

        public static INode GetNode(this IModelIndex index, NodeLink nodeLink)
            => index.TryGetNode(nodeLink) ?? throw new KeyNotFoundException();

        // [Try]GetNode<T>

        [return: MaybeNull]
        public static T TryGetNode<T>(this IModelIndex index, Key key)
            where T : class, INode
            => (T) index.TryGetNode(key)!;

        [return: MaybeNull]
        public static T TryGetNode<T>(this IModelIndex index, NodeLink nodeLink)
            where T : class, INode
            => (T) index.TryGetNode(nodeLink)!;

        public static T GetNode<T>(this IModelIndex index, Key key)
            where T : class, INode
            => (T) index.GetNode(key);

        public static T GetNode<T>(this IModelIndex index, NodeLink nodeLink)
            where T : class, INode
            => (T) index.GetNode(nodeLink);

        // With

        public static (TIndex Index, ModelChangeSet ChangeSet) With<TIndex>(this TIndex index, INode source, INode target)
            where TIndex : IModelIndex
        {
            var (i, cs) = index.BaseWith(source, target);
            return ((TIndex) i, cs);
        }
    }
}
