using System;
using System.Collections.Generic;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    public static class ExtendableNodeEx
    {
        public static readonly string PropertyKeyPrefix = "@Ext_";

        public static Symbol GetExtensionKey(Type type) => ExtensionKeyCache.Get(type);

        // GetExtension(s)

        public static object? GetExt(this IExtendableNode node, Type extensionType)
        {
            var localKey = GetExtensionKey(extensionType);
            return node.TryGetValue(localKey, out var e) ? e : null;
        }

        public static TExtension GetExt<TExtension>(this IExtendableNode node, TExtension @default = default)
        {
            var localKey = GetExtensionKey(typeof(TExtension));
            if (node.TryGetValue(localKey, out var e))
                return (TExtension) e!;
            return @default;
        }

        // GetAllExtensions

        public static IEnumerable<(Symbol PropertyKey, object? Value)> GetAllExt(
            this IExtendableNode node)
        {
            foreach (var (k, v) in node.Items)
                if (k.Value.StartsWith(PropertyKeyPrefix))
                    yield return (k, v);
        }

        // WithExt

        public static IExtendableNode WithExt(this IExtendableNode node, Type extensionType, object extension)
        {
            var localKey = GetExtensionKey(extensionType);
            return node.BaseWithExt(localKey, extension);
        }

        public static IExtendableNode WithExt<TExtension>(this IExtendableNode node, TExtension extension)
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExt(typeof(TExtension), extension!);

        public static TNode WithExt<TNode>(this TNode node, Type extensionType, object extension)
            where TNode : class, IExtendableNode
        {
            var localKey = GetExtensionKey(extensionType);
            return node.With(localKey, extension);
        }
        
        public static TNode WithExt<TNode, TExtension>(this TNode node, TExtension extension)
            where TNode : class, IExtendableNode
            // ReSharper disable once HeapView.BoxingAllocation
            => node.WithExt(typeof(TExtension), extension!);
        
        // WithoutExt

        public static IExtendableNode WithoutExt(this IExtendableNode node, Type extensionType)
        {
            var localKey = GetExtensionKey(extensionType);
            return node.BaseWithExt(localKey, null);
        }
        
        public static IExtendableNode WithoutExt<TExtension>(this IExtendableNode node)
            => node.WithoutExt(typeof(TExtension));
        
        public static TNode WithoutExt<TNode>(this TNode node, Type extensionType)
            where TNode : class, IExtendableNode 
            => node.WithoutExt(extensionType);

        public static TNode WithoutExt<TNode, TExtension>(this TNode node)
            where TNode : class, IExtendableNode
            => node.WithoutExt(typeof(TExtension));

        // WithAllExt

        public static IExtendableNode WithAllExt(this IExtendableNode target, IExtendableNode source)
            => target.BaseWithAllExt(source.GetAllExt());

        public static TNode WithAllExt<TNode>(this TNode target, IExtendableNode source)
            where TNode : class, IExtendableNode
            => (TNode) WithAllExt((IExtendableNode) target, source);
    }
}
