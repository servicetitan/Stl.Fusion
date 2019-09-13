using System.Collections.Generic;

namespace Stl.ImmutableModel
{
    public static class SimpleNodeEx
    {
        // "With" overloads
        
        public static TNode With<TNode>(this TNode node, Symbol key, object? value)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(key, value);

        public static TNode With<TNode, T>(this TNode node, params (Symbol Key, object? Value)[] changes)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(changes);

        public static TNode With<TNode, T>(this TNode node, IEnumerable<(Symbol Key, object? Value)> changes)
            where TNode : class, ISimpleNode
            => (TNode) node.BaseWith(changes);
    }
}
