using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Stl.ImmutableModel
{
    public static class IndexEx
    {
        // (Try)GetXxx
        
        public static SymbolPath GetPath(this IIndex index, INode node) 
            => index.TryGetPath(node) ?? throw new KeyNotFoundException();
        
        [return: MaybeNull]
        public static T TryGetNode<T>(this IIndex index, SymbolPath path)
            where T : class, INode
            => (T) index.TryGetNode(path)!;
        
        [return: MaybeNull]
        public static T TryGetNode<T>(this IIndex index, DomainKey domainKey)
            where T : class, INode
            => (T) index.TryGetNode(domainKey)!;
        
        public static INode GetNode(this IIndex index, SymbolPath path)
            => index.TryGetNode(path) ?? throw new KeyNotFoundException();
        
        public static INode GetNode(this IIndex index, DomainKey domainKey)
            => index.TryGetNode(domainKey) ?? throw new KeyNotFoundException();
        
        public static T GetNode<T>(this IIndex index, SymbolPath path)
            where T : class, INode
            => (T) index.GetNode(path);

        public static T GetNode<T>(this IIndex index, DomainKey domainKey)
            where T : class, INode
            => (T) index.GetNode(domainKey);

        // (Try)Resolve also resolve properties via path 

        public static Option<object?> TryResolve(this IIndex index, SymbolPath path)
        {
            if (path.Head == null)
                // Root resolution
                return Option.FromClass((object?) index.TryGetNode(path));
            var node = index.TryGetNode(path.Head);
            return node?.DualGetUntyped(path.Tail) ?? default;
        }

        public static Option<T> TryResolve<T>(this IIndex index, SymbolPath path)
        {
            var option = index.TryResolve(path);
            return option.HasValue ? Option.Some((T) option.Value!) : default;
        }

        public static object? Resolve(this IIndex index, SymbolPath path)
            => index.TryResolve(path).ValueOr(() => throw new KeyNotFoundException());

        public static T Resolve<T>(this IIndex index, SymbolPath path)
            => (T) index.TryResolve(path).ValueOr(() => throw new KeyNotFoundException())!;
    }
}
