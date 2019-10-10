using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stl.ImmutableModel.Updating;

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

        // Update

        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, INode target)
            where TIndex : IIndex
        {
            var (i, cs) = index.BaseUpdate(source, target);
            return ((TIndex) i, cs);
        }

        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, INode source, Symbol key, Option<object?> value)
            where TIndex : IIndex
            => index.Update(source, source.DualWith(key, value));
        
        public static (TIndex Index, ModelChangeSet ChangeSet) Update<TIndex>(this TIndex index, SymbolList list, Option<object?> value)
            where TIndex : IIndex
        {
            if (list.Head == null)
                // Root update
                return index.Update(index.GetNodeByPath(list), (INode) value.Value!);
            var source = index.GetNodeByPath(list.Head);
            var target = source.DualWith(list.Tail, value);
            return index.Update(source, target);
        }
    }
}
