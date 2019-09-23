using System;
using System.Collections.Generic;
using System.Linq;

namespace Stl.ImmutableModel
{
    public static class NodeEx
    {
        public static IEnumerable<KeyValuePair<Symbol, object?>> DualGetItems(this INode node) 
            => node switch {
                ICollectionNode c => c.Keys.Select(k =>
                    new KeyValuePair<Symbol, object?>(k.Symbol, c.GetUntyped(k).UnsafeValue)),
                ISimpleNode s => s,
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };

        public static IEnumerable<KeyValuePair<Symbol, INode>> DualGetNodeItems(this INode node)
        {
            foreach (var (k, v) in node.DualGetItems()) {
                if (v is INode n)
                    yield return new KeyValuePair<Symbol, INode>(k, n);
            }
        }

        // Dual* methods

        public static Option<object?> DualGetUntyped(this INode node, Symbol key)
            => node switch {
                ICollectionNode c => c.GetUntyped(key),
                ISimpleNode s => s.GetUntyped(key),
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };
        
        public static TNode DualWith<TNode>(this TNode node, Symbol key, Option<object?> value)
            where TNode : class, INode
            => (TNode) (node switch {
                ICollectionNode c => (INode) c.BaseWith(new LocalKey(key), value),
                ISimpleNode s => s.With(key, value.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(node)),
            });

        public static TNode DualWith<TNode>(this TNode node, IEnumerable<(Symbol Key, Option<object?> Value)> changes)
            where TNode : class, INode
            => (TNode) (node switch {
                ICollectionNode c => (INode) c.BaseWith(changes.Select(p => (new LocalKey(p.Key), p.Value))),
                ISimpleNode s => s.BaseWith(changes.Select(p => (p.Key, p.Value.Value))),
                _ => throw new ArgumentOutOfRangeException(nameof(node)),
            });
    }
}
