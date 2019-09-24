using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Stl.ImmutableModel.Indexing
{
    public static class IndexEx
    {
        // GetParent

        public static INode? GetParent(this IIndex index, INode node)
        {
            var parentPath = index.GetPath(node).Head;
            return parentPath == null ? null : index.GetNodeByPath(parentPath);
        }

        // GetPath

        public static SymbolList GetPath(this IIndex index, INode node) 
            => index.TryGetPath(node) ?? throw new KeyNotFoundException();
        
        // GetNode[ByPath] (non-generic)

        public static INode GetNode(this IIndex index, Key key)
            => index.TryGetNode(key) ?? throw new KeyNotFoundException();

        public static INode GetNodeByPath(this IIndex index, SymbolList list)
            => index.TryGetNodeByPath(list) ?? throw new KeyNotFoundException();

        // [Try]GetNode[ByPath]<T>

        [return: MaybeNull]
        public static T TryGetNode<T>(this IIndex index, Key key)
            where T : class, INode
            => (T) index.TryGetNode(key)!;

        [return: MaybeNull]
        public static T TryGetNodeByPath<T>(this IIndex index, SymbolList list)
            where T : class, INode
            => (T) index.TryGetNodeByPath(list)!;

        public static T GetNode<T>(this IIndex index, Key key)
            where T : class, INode
            => (T) index.GetNode(key);

        public static T GetNodeByPath<T>(this IIndex index, SymbolList list)
            where T : class, INode
            => (T) index.GetNodeByPath(list);
    }
}
