using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    public static class Ext
    {
        public static readonly string PropertyKeyPrefix = "@Ext_";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Symbol GetPropertyKey(Type type) => ExtCache.GetPropertyKey(type);

        // GetExtension(s)

        public static object? GetExt(this ISimpleNode node, Type extensionType)
        {
            var localKey = GetPropertyKey(extensionType);
            return node.TryGetValue(localKey, out var e) ? e : null;
        }

        public static TExtension GetExt<TExtension>(this ISimpleNode node)
        {
            var localKey = GetPropertyKey(typeof(TExtension));
            if (node.TryGetValue(localKey, out var e))
                return (TExtension) e!;
            return default!;
        }

        // GetAllExtensions

        public static IEnumerable<(Symbol PropertyKey, object? Value)> GetAllExt(
            this ISimpleNode node)
        {
            foreach (var (k, v) in node.Items)
                if (k.Value.StartsWith(PropertyKeyPrefix))
                    yield return (k, v);
        }

        // WithExtension

        public static ISimpleNode WithExt(this ISimpleNode node, Type extensionType, object extension)
        {
            var localKey = GetPropertyKey(extensionType);
            return node.BaseWith(localKey, extension);
        }

        public static ISimpleNode WithExt<TExtension>(this ISimpleNode node, TExtension extension)
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExt(typeof(TExtension), extension!);

        public static TNode WithExt<TNode>(this TNode node, Type extensionType, object extension)
            where TNode : class, ISimpleNode
        {
            var localKey = GetPropertyKey(extensionType);
            return node.With(localKey, extension);
        }
        
        public static TNode WithExt<TNode, TExtension>(this TNode node, TExtension extension)
            where TNode : class, ISimpleNode
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExt(typeof(TExtension), extension!);
        
        // WithoutExtension

        public static ISimpleNode WithoutExt(this ISimpleNode node, Type extensionType)
        {
            var localKey = GetPropertyKey(extensionType);
            return node.BaseWithout(localKey);
        }
        
        public static ISimpleNode WithoutExt<TExtension>(this ISimpleNode node)
            => node.WithoutExt(typeof(TExtension));
        
        public static TNode WithoutExt<TNode>(this TNode node, Type extensionType)
            where TNode : class, ISimpleNode
        {
            var localKey = GetPropertyKey(extensionType);
            return node.Without(localKey);
        }
        
        public static TNode WithoutExt<TNode, TExtension>(this TNode node)
            where TNode : class, ISimpleNode
            => node.WithoutExt(typeof(TExtension));

        // WithAllExtensions

        public static ISimpleNode WithAllExt(this ISimpleNode target, ISimpleNode source)
            => target.BaseWith(source.GetAllExt());

        public static TNode WithAllExt<TNode>(this TNode target, ISimpleNode source)
            where TNode : class, ISimpleNode
            => (TNode) WithAllExt((ISimpleNode) target, source);
    }
}
