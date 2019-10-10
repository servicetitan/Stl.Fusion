using System.Collections.Generic;

namespace Stl.ImmutableModel
{
    public static class SimpleNodeEx
    {
        // "With" overloads
        
        public static TNode With<TNode>(this TNode node, Symbol propertyKey, object? value)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(propertyKey, value);

        public static TNode With<TNode, T>(this TNode node, params (Symbol PropertyKey, object? Value)[] changes)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(changes);

        public static TNode With<TNode, T>(this TNode node, IEnumerable<(Symbol PropertyKey, object? Value)> changes)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(changes);
    }
}
