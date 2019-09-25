using System;
using System.Collections.Generic;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    public static class Extensions
    {
        // GetExtension

        public static object? GetExtension(this ISimpleNode node, Type extensionType)
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(extensionType);
            return node.TryGetValue(localKey, out var e) ? e : null;
        }

        public static TExtension GetExtension<TExtension>(this ISimpleNode node)
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(typeof(TExtension));
            if (node.TryGetValue(localKey, out var e))
                return (TExtension) e!;
            return default!;
        }

        public static IEnumerable<KeyValuePair<Symbol, object?>> GetExtensions(this ISimpleNode node)
        {
            foreach (var pair in node) {
                if (pair.Key.Value.StartsWith(ExtensionKeyProvider.ExtensionPrefix))
                    yield return pair;
            }
        }

        // WithExtension

        public static ISimpleNode WithExtension(this ISimpleNode node, Type extensionType, object extension)
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(extensionType);
            return node.BaseWith(localKey, extension);
        }

        public static ISimpleNode WithExtension<TExtension>(this ISimpleNode node, TExtension extension)
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExtension(typeof(TExtension), extension!);

        public static TNode WithExtension<TNode>(this TNode node, Type extensionType, object extension)
            where TNode : class, ISimpleNode
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(extensionType);
            return node.With(localKey, extension);
        }
        
        public static TNode WithExtension<TNode, TExtension>(this TNode node, TExtension extension)
            where TNode : class, ISimpleNode
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExtension(typeof(TExtension), extension!);
        
        // WithoutExtension

        public static ISimpleNode WithoutExtension(this ISimpleNode node, Type extensionType)
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(extensionType);
            return node.BaseWithout(localKey);
        }
        
        public static ISimpleNode WithoutExtension<TExtension>(this ISimpleNode node)
            => node.WithoutExtension(typeof(TExtension));
        
        public static TNode WithoutExtension<TNode>(this TNode node, Type extensionType)
            where TNode : class, ISimpleNode
        {
            var localKey = ExtensionKeyProvider.GetLocalKey(extensionType);
            return node.Without(localKey);
        }
        
        public static TNode WithoutExtension<TNode, TExtension>(this TNode node)
            where TNode : class, ISimpleNode
            => node.WithoutExtension(typeof(TExtension));
    }
}
