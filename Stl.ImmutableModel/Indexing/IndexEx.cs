using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Stl.ImmutableModel.Indexing
{
    public static class IndexEx
    {
        // GetPath

        public static SymbolPath GetPath(this IIndex index, INode node) 
            => index.TryGetPath(node) ?? throw new KeyNotFoundException();
        
        // [Try]GetNode[ByPath]
        
        [return: MaybeNull]
        public static T TryGetNodeByPath<T>(this IIndex index, SymbolPath path)
            where T : class, INode
            => (T) index.TryGetNodeByPath(path)!;
        
        [return: MaybeNull]
        public static T TryGetNode<T>(this IIndex index, Key key)
            where T : class, INode
            => (T) index.TryGetNode(key)!;
        
        public static INode GetNodeByPath(this IIndex index, SymbolPath path)
            => index.TryGetNodeByPath(path) ?? throw new KeyNotFoundException();
        
        public static INode GetNode(this IIndex index, Key key)
            => index.TryGetNode(key) ?? throw new KeyNotFoundException();
        
        public static T GetNodeByPath<T>(this IIndex index, SymbolPath path)
            where T : class, INode
            => (T) index.GetNodeByPath(path);

        public static T GetNode<T>(this IIndex index, Key key)
            where T : class, INode
            => (T) index.GetNode(key);
    }
}
