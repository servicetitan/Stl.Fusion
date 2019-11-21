using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stl.ImmutableModel.Updating;
using Stl.Text;

namespace Stl.ImmutableModel.Indexing
{
    public static class ModelIndexEx
    {
        // GetParent

        public static INode? GetParent(this IModelIndex index, INode node)
        {
            var parentPath = index.GetPath(node).Prefix;
            return parentPath == null ? null : index.GetNodeByPath(parentPath);
        }

        // GetPath

        public static SymbolList GetPath(this IModelIndex index, INode node) 
            => index.TryGetPath(node) ?? throw new KeyNotFoundException();
        
        // GetNode[ByPath] (non-generic)

        public static INode GetNode(this IModelIndex index, Key key)
            => index.TryGetNode(key) ?? throw new KeyNotFoundException();

        public static INode GetNodeByPath(this IModelIndex index, SymbolList list)
            => index.TryGetNodeByPath(list) ?? throw new KeyNotFoundException();

        // [Try]GetNode[ByPath]<T>

        [return: MaybeNull]
        public static T TryGetNode<T>(this IModelIndex index, Key key)
            where T : class, INode
            => (T) index.TryGetNode(key)!;

        [return: MaybeNull]
        public static T TryGetNodeByPath<T>(this IModelIndex index, SymbolList list)
            where T : class, INode
            => (T) index.TryGetNodeByPath(list)!;

        public static T GetNode<T>(this IModelIndex index, Key key)
            where T : class, INode
            => (T) index.GetNode(key);

        public static T GetNodeByPath<T>(this IModelIndex index, SymbolList list)
            where T : class, INode
            => (T) index.GetNodeByPath(list);

        // With

        public static (TIndex Index, ModelChangeSet ChangeSet) With<TIndex>(this TIndex index, INode source, INode target)
            where TIndex : IModelIndex
        {
            var (i, cs) = index.BaseWith(source, target);
            return ((TIndex) i, cs);
        }
    }
}
