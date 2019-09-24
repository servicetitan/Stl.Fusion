using System;
using System.Collections.Generic;
using System.Linq;

namespace Stl.ImmutableModel
{
    public static class NodeEx
    {
        // Dual* methods

        public static IEnumerable<KeyValuePair<Symbol, object?>> DualGetItems(this INode node) 
            => node switch {
                ICollectionNode c => c.Keys.Select(localKey =>
                    new KeyValuePair<Symbol, object?>(localKey, c.GetValueUntyped(localKey).UnsafeValue)),
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

        public static Option<object?> DualGetValueUntyped(this INode node, Symbol localKey)
            => node switch {
                ICollectionNode c => c.GetValueUntyped(localKey),
                ISimpleNode s => s.GetValueUntyped(localKey),
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };
        
        public static TNode DualWith<TNode>(this TNode node, Symbol localKey, Option<object?> value)
            where TNode : class, INode
            => (TNode) (node switch {
                ICollectionNode c => (INode) c.BaseWith(localKey, value),
                ISimpleNode s => s.With(localKey, value.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(node)),
            });

        public static TNode DualWith<TNode>(this TNode node, IEnumerable<(Symbol LocalKey, Option<object?> Value)> changes)
            where TNode : class, INode
            => (TNode) (node switch {
                ICollectionNode c => (INode) c.BaseWith(changes.Select(p => (p.LocalKey, p.Value))),
                ISimpleNode s => s.BaseWith(changes.Select(p => (p.LocalKey, p.Value.Value))),
                _ => throw new ArgumentOutOfRangeException(nameof(node)),
            });
    }
}
